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
    public class LrManagementContractTests
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
                new FreightPaymentTypeConverter(),
                new LrStatusConverter(),
                new EwayStatusConverter()
            }
        };

        [Fact]
        public void LrEnums_serialize_and_deserialize_correctly()
        {
            var toPayJson = JsonSerializer.Serialize(FreightPaymentType.ToPay, _jsonOptions);
            Assert.Equal("\"To Pay\"", toPayJson);
            var toPay = JsonSerializer.Deserialize<FreightPaymentType>(toPayJson, _jsonOptions);
            Assert.Equal(FreightPaymentType.ToPay, toPay);

            var toBeBilledJson = JsonSerializer.Serialize(FreightPaymentType.ToBeBilled, _jsonOptions);
            Assert.Equal("\"To Be Billed\"", toBeBilledJson);
            var toBeBilled = JsonSerializer.Deserialize<FreightPaymentType>(toBeBilledJson, _jsonOptions);
            Assert.Equal(FreightPaymentType.ToBeBilled, toBeBilled);

            var statusJson = JsonSerializer.Serialize(LrStatus.Generated, _jsonOptions);
            Assert.Equal("\"Generated\"", statusJson);
            var status = JsonSerializer.Deserialize<LrStatus>(statusJson, _jsonOptions);
            Assert.Equal(LrStatus.Generated, status);
        }

        [Fact]
        public async Task CreateLr_creates_record_with_numbering_and_timeline()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchId = 1,
                DispatchNumber = "DN-2026-0001",
                TransportId = 2,
                TransportNumber = "TR-2026-0002",
                VehicleNumber = "MH-12-PQ-5678",
                CustomerId = 10,
                CustomerName = "Apex Heavy Engineering",
                LrDate = "2026-08-20",
                Consignor = "CodingEra Manufacturing Pvt Ltd",
                Consignee = "Apex Heavy Engineering, Mumbai Works",
                Packages = 12,
                WeightKg = 1500.50m,
                FreightCharges = 18500.00m,
                PaymentType = FreightPaymentType.ToPay,
                Remarks = "Handle with crane only",
                Notes = "Driver copy handed over"
            };

            var result = await service.CreateLrAsync(payload, "TestUser");

            Assert.NotNull(result);
            Assert.StartsWith("LR-", result.LrNumber);
            Assert.Equal(LrStatus.Draft, result.Status);
            Assert.Equal("Apex Heavy Engineering", result.CustomerName);
            Assert.Equal(12, result.Packages);
            Assert.Equal(1500.50m, result.WeightKg);
            Assert.Single(result.Timeline);
            Assert.Equal("LR created", result.Timeline[0].Action);
        }

        [Theory]
        [InlineData(0, 100, "Packages must be greater than 0")]
        [InlineData(5, 0, "Weight must be greater than 0")]
        [InlineData(5, -10, "Weight must be greater than 0")]
        public async Task CreateLr_validates_packages_and_weight(int packages, decimal weight, string expectedError)
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchNumber = "DN-1",
                TransportNumber = "TR-1",
                VehicleNumber = "MH-12-1234",
                CustomerName = "Cust",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = packages,
                WeightKg = weight
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLrAsync(payload, "User"));
            Assert.Contains(expectedError, ex.Message);
        }

        [Fact]
        public async Task StatusWorkflow_transitions_from_draft_to_generated_issued_closed()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchNumber = "DN-1",
                TransportNumber = "TR-1",
                VehicleNumber = "MH-12-1234",
                CustomerName = "Cust",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 5,
                WeightKg = 250,
                FreightCharges = 1000
            };

            var lr = await service.CreateLrAsync(payload, "User");
            Assert.Equal(LrStatus.Draft, lr.Status);

            // Draft -> Generated
            lr = await service.GenerateLrDocumentAsync(lr.Id, new StatusActionRequestDto { Remarks = "Document generated" }, "Manager");
            Assert.Equal(LrStatus.Generated, lr.Status);

            // Generated -> Issued
            lr = await service.IssueLrAsync(lr.Id, new StatusActionRequestDto { Remarks = "Issued to driver" }, "Dispatcher");
            Assert.Equal(LrStatus.Issued, lr.Status);

            // Issued -> Closed
            lr = await service.CloseLrAsync(lr.Id, new StatusActionRequestDto { Remarks = "Signed copy received" }, "Auditor");
            Assert.Equal(LrStatus.Closed, lr.Status);

            Assert.Equal(4, lr.Timeline.Count);
        }

        [Fact]
        public async Task StatusWorkflow_rejects_invalid_transitions()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchNumber = "DN-1",
                TransportNumber = "TR-1",
                VehicleNumber = "MH-12-1234",
                CustomerName = "Cust",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 5,
                WeightKg = 250
            };

            var lr = await service.CreateLrAsync(payload, "User");

            // Attempt invalid Draft -> Issued directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.IssueLrAsync(lr.Id, null, "User"));
        }

        [Fact]
        public async Task Update_and_Delete_allowed_only_in_draft()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchNumber = "DN-1",
                TransportNumber = "TR-1",
                VehicleNumber = "MH-12-1234",
                CustomerName = "Cust",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 5,
                WeightKg = 250
            };

            var lr = await service.CreateLrAsync(payload, "User");
            await service.GenerateLrDocumentAsync(lr.Id, null, "User");

            var updatePayload = new LrUpdateRequestDto
            {
                DispatchNumber = "DN-1",
                TransportNumber = "TR-1",
                VehicleNumber = "MH-12-1234",
                CustomerName = "Cust Updated",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 6,
                WeightKg = 300
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateLrAsync(lr.Id, updatePayload, "User"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteLrAsync(lr.Id));
        }

        [Fact]
        public async Task GenerateEway_creates_linked_eway_bill_when_generated_or_issued()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            var payload = new LrCreateRequestDto
            {
                DispatchId = 5,
                DispatchNumber = "DN-2026-0005",
                TransportId = 8,
                TransportNumber = "TR-2026-0008",
                VehicleNumber = "MH-14-GH-9988",
                CustomerId = 15,
                CustomerName = "Tata Motors",
                LrDate = "2026-08-20",
                Consignor = "CodingEra Manufacturing",
                Consignee = "Tata Motors Plant",
                Packages = 20,
                WeightKg = 3000
            };

            var lr = await service.CreateLrAsync(payload, "User");

            // Attempt on Draft -> fails
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GenerateEwayFromLrAsync(lr.Id, "User"));

            // Move to Generated
            await service.GenerateLrDocumentAsync(lr.Id, null, "User");

            var eway = await service.GenerateEwayFromLrAsync(lr.Id, "User");

            Assert.NotNull(eway);
            Assert.StartsWith("EWB-", eway.EwayBillNumber);
            Assert.Equal("Tata Motors", eway.CustomerName);
            Assert.Equal("MH-14-GH-9988", eway.VehicleNumber);
            Assert.Equal(EwayStatus.Generated, eway.Status);

            // Calling again returns existing active E-Way bill
            var eway2 = await service.GenerateEwayFromLrAsync(lr.Id, "User");
            Assert.Equal(eway.Id, eway2.Id);
            Assert.Equal(eway.EwayBillNumber, eway2.EwayBillNumber);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_kpis()
        {
            using var db = CreateDbContext();
            var lrNumbering = new LrNumberingService(db);
            var ewayNumbering = new EwayNumberingService(db);
            var service = new LrManagementService(db, lrNumbering, ewayNumbering);

            // Draft
            await service.CreateLrAsync(new LrCreateRequestDto
            {
                DispatchNumber = "D1",
                TransportNumber = "T1",
                VehicleNumber = "V1",
                CustomerName = "C1",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 1,
                WeightKg = 10
            }, "User");

            // Generated
            var lr2 = await service.CreateLrAsync(new LrCreateRequestDto
            {
                DispatchNumber = "D2",
                TransportNumber = "T2",
                VehicleNumber = "V2",
                CustomerName = "C2",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 1,
                WeightKg = 10
            }, "User");
            await service.GenerateLrDocumentAsync(lr2.Id, null, "User");

            // Issued
            var lr3 = await service.CreateLrAsync(new LrCreateRequestDto
            {
                DispatchNumber = "D3",
                TransportNumber = "T3",
                VehicleNumber = "V3",
                CustomerName = "C3",
                LrDate = "2026-08-20",
                Consignor = "A",
                Consignee = "B",
                Packages = 1,
                WeightKg = 10
            }, "User");
            await service.GenerateLrDocumentAsync(lr3.Id, null, "User");
            await service.IssueLrAsync(lr3.Id, null, "User");

            var dashboard = await service.GetLrDashboardAsync();

            Assert.Equal(2, dashboard.Generated); // Generated + Issued (lr2, lr3)
            Assert.Equal(1, dashboard.Issued);    // lr3
            Assert.Equal(1, dashboard.Pending);   // lr1 (Draft)
        }
    }
}
