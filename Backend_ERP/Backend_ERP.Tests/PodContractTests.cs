using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PodContractTests
    {
        private ERPDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters =
            {
                new PodStatusConverter()
            }
        };

        [Fact]
        public void PodStatus_serializes_and_deserializes_correctly()
        {
            var pendingJson = JsonSerializer.Serialize(PodStatus.Pending, _jsonOptions);
            Assert.Equal("\"Pending\"", pendingJson);
            var pending = JsonSerializer.Deserialize<PodStatus>(pendingJson, _jsonOptions);
            Assert.Equal(PodStatus.Pending, pending);

            var confirmedJson = JsonSerializer.Serialize(PodStatus.Confirmed, _jsonOptions);
            Assert.Equal("\"Confirmed\"", confirmedJson);
            var confirmed = JsonSerializer.Deserialize<PodStatus>(confirmedJson, _jsonOptions);
            Assert.Equal(PodStatus.Confirmed, confirmed);
        }

        [Fact]
        public async Task CreatePod_creates_record_with_numbering_attachments_and_timeline()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchId = 1,
                DispatchNumber = "DN-2026-0001",
                CustomerId = 10,
                CustomerName = "Apex Heavy Engineering",
                DeliveryDate = "2026-08-20",
                ReceiverName = "John Doe",
                ReceiverContact = "+91 98765 43210",
                DeliveryRemarks = "All 5 cartons intact",
                ProofOfDelivery = new List<DispatchAttachmentDto>
                {
                    new DispatchAttachmentDto { Id = "att-1", Name = "signed_challan.pdf", SizeKb = 120, UploadedBy = "Driver" }
                },
                Signature = new List<DispatchAttachmentDto>
                {
                    new DispatchAttachmentDto { Id = "sig-1", Name = "receiver_sign.png", SizeKb = 45, UploadedBy = "Driver" }
                },
                Photos = new List<DispatchAttachmentDto>
                {
                    new DispatchAttachmentDto { Id = "ph-1", Name = "unloading_photo.jpg", SizeKb = 250, UploadedBy = "Driver" }
                }
            };

            var result = await service.CreatePodAsync(payload, "DriverUser");

            Assert.NotNull(result);
            Assert.StartsWith("POD-", result.PodNumber);
            Assert.Equal(PodStatus.Pending, result.Status);
            Assert.Equal("Apex Heavy Engineering", result.CustomerName);
            Assert.Single(result.ProofOfDelivery);
            Assert.Single(result.Signature);
            Assert.Single(result.Photos);
            Assert.Single(result.Timeline);
            Assert.Equal("POD created", result.Timeline[0].Action);
        }

        [Fact]
        public async Task CreatePod_validates_receiver_name_and_contact()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchNumber = "DN-1",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "",
                ReceiverContact = ""
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePodAsync(payload, "User"));
            Assert.Contains("Receiver name and contact are required", ex.Message);
        }

        [Fact]
        public async Task StatusWorkflow_transitions_from_pending_to_delivered_confirmed_closed()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchNumber = "DN-1",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "John Doe",
                ReceiverContact = "9876543210"
            };

            var pod = await service.CreatePodAsync(payload, "User");
            Assert.Equal(PodStatus.Pending, pod.Status);

            // Pending -> Delivered
            pod = await service.MarkPodDeliveredAsync(pod.Id, new StatusActionRequestDto { Remarks = "Goods delivered at site" }, "Driver");
            Assert.Equal(PodStatus.Delivered, pod.Status);

            // Delivered -> Confirmed
            pod = await service.ConfirmDeliveryAsync(pod.Id, new StatusActionRequestDto { Remarks = "Receiver verified materials" }, "LogisticsManager");
            Assert.Equal(PodStatus.Confirmed, pod.Status);

            // Confirmed -> Closed
            pod = await service.ClosePodAsync(pod.Id, new StatusActionRequestDto { Remarks = "Billing cleared, POD closed" }, "FinanceManager");
            Assert.Equal(PodStatus.Closed, pod.Status);

            Assert.Equal(4, pod.Timeline.Count);
        }

        [Fact]
        public async Task StatusWorkflow_rejects_invalid_transitions()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchNumber = "DN-1",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "John Doe",
                ReceiverContact = "9876543210"
            };

            var pod = await service.CreatePodAsync(payload, "User");

            // Attempt invalid Pending -> Confirmed directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConfirmDeliveryAsync(pod.Id, null, "User"));

            // Attempt invalid Pending -> Close directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ClosePodAsync(pod.Id, null, "User"));
        }

        [Fact]
        public async Task Update_and_Delete_allowed_only_in_pending()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchNumber = "DN-1",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "John Doe",
                ReceiverContact = "9876543210"
            };

            var pod = await service.CreatePodAsync(payload, "User");
            await service.MarkPodDeliveredAsync(pod.Id, null, "User");

            var updatePayload = new PodUpdateRequestDto
            {
                DispatchNumber = "DN-1",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "Jane Doe",
                ReceiverContact = "9876543210"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdatePodAsync(pod.Id, updatePayload, "User"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeletePodAsync(pod.Id));
        }

        [Fact]
        public async Task DuplicatePod_for_active_dispatch_is_prevented()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            var payload = new PodCreateRequestDto
            {
                DispatchId = 15,
                DispatchNumber = "DN-2026-0015",
                CustomerName = "Cust",
                DeliveryDate = "2026-08-20",
                ReceiverName = "John",
                ReceiverContact = "9876543210"
            };

            await service.CreatePodAsync(payload, "User");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePodAsync(payload, "User"));
            Assert.Contains("already exists for dispatch DN-2026-0015", ex.Message);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_kpis()
        {
            using var db = CreateDbContext();
            var numbering = new PodNumberingService(db);
            var service = new PodService(db, numbering);

            // 1 Pending
            await service.CreatePodAsync(new PodCreateRequestDto
            {
                DispatchNumber = "D1",
                CustomerName = "C1",
                DeliveryDate = "2026-08-20",
                ReceiverName = "R1",
                ReceiverContact = "C1"
            }, "User");

            // 1 Delivered
            var p2 = await service.CreatePodAsync(new PodCreateRequestDto
            {
                DispatchNumber = "D2",
                CustomerName = "C2",
                DeliveryDate = "2026-08-20",
                ReceiverName = "R2",
                ReceiverContact = "C2"
            }, "User");
            await service.MarkPodDeliveredAsync(p2.Id, null, "User");

            // 1 Confirmed
            var p3 = await service.CreatePodAsync(new PodCreateRequestDto
            {
                DispatchNumber = "D3",
                CustomerName = "C3",
                DeliveryDate = "2026-08-20",
                ReceiverName = "R3",
                ReceiverContact = "C3"
            }, "User");
            await service.MarkPodDeliveredAsync(p3.Id, null, "User");
            await service.ConfirmDeliveryAsync(p3.Id, null, "User");

            var dashboard = await service.GetPodDashboardAsync();

            Assert.Equal(1, dashboard.PendingPod);
            Assert.Equal(1, dashboard.Delivered);
            Assert.Equal(1, dashboard.Confirmed);
        }
    }
}
