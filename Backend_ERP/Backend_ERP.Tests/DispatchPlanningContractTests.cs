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
    public class DispatchPlanningContractTests
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
                new DispatchPlanStatusConverter(),
                new DispatchPriorityConverter()
            }
        };

        [Fact]
        public void DispatchPlanStatusConverter_serializes_and_deserializes_correctly()
        {
            var status = DispatchPlanStatus.ReadyForDispatch;
            var json = JsonSerializer.Serialize(status, _jsonOptions);
            Assert.Equal("\"Ready For Dispatch\"", json);

            var deserialized = JsonSerializer.Deserialize<DispatchPlanStatus>(json, _jsonOptions);
            Assert.Equal(DispatchPlanStatus.ReadyForDispatch, deserialized);

            var draftJson = JsonSerializer.Serialize(DispatchPlanStatus.Draft, _jsonOptions);
            Assert.Equal("\"Draft\"", draftJson);
        }

        [Fact]
        public async Task CreateDispatchPlan_creates_record_with_numbering_items_and_timeline()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerId = 1,
                CustomerName = "Acme Steel Traders",
                SalesOrderId = 1,
                SalesOrderNumber = "SO-2026-001",
                DeliveryAddress = "Plot 12, MIDC Industrial Area, Pune",
                WarehouseId = 2,
                WarehouseName = "FG Warehouse A",
                Priority = DispatchPriority.High,
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                VehicleRequired = true,
                Remarks = "Urgent shipment",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new()
                    {
                        FinishedGoodId = 101,
                        FinishedGoodCode = "FG-BRG-6205",
                        FinishedGoodName = "Bearing Housing 6205",
                        BatchNumber = "FG-BAT-4412",
                        Quantity = 200,
                        Uom = "NOS",
                        WarehouseId = 2,
                        WarehouseName = "FG Warehouse A",
                        FinalInspectionId = 1,
                        FinalInspectionNumber = "FI-2026-001",
                        TestCertificateId = 1,
                        TestCertificateNumber = "TC-2026-001"
                    }
                }
            };

            var result = await service.CreateDispatchPlanAsync(payload, "TestUser");

            Assert.NotNull(result);
            Assert.StartsWith("DN-", result.DispatchNumber);
            Assert.Equal(DispatchPlanStatus.Draft, result.Status);
            Assert.Equal("Acme Steel Traders", result.CustomerName);
            Assert.Single(result.Items);
            Assert.Equal("FG-BRG-6205", result.Items[0].FinishedGoodCode);
            Assert.Single(result.Timeline);
            Assert.Equal("Created dispatch plan", result.Timeline[0].Action);
        }

        [Fact]
        public async Task CreateDispatchPlan_throws_when_plannedDate_is_after_expectedDate()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Customer",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Address",
                WarehouseName = "Warehouse",
                PlannedDispatchDate = "2026-08-25",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 10 }
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDispatchPlanAsync(payload, "User"));
        }

        [Fact]
        public async Task StatusWorkflow_transitions_through_full_lifecycle()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Acme",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Site 1",
                WarehouseName = "FG Store",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 5 }
                }
            };

            var plan = await service.CreateDispatchPlanAsync(payload, "User");
            Assert.Equal(DispatchPlanStatus.Draft, plan.Status);

            // Draft -> Planned
            plan = await service.PlanDispatchAsync(plan.Id, new StatusActionRequestDto { Remarks = "Planned" }, "Planner");
            Assert.Equal(DispatchPlanStatus.Planned, plan.Status);

            // Planned -> Approved
            plan = await service.ApproveDispatchAsync(plan.Id, new StatusActionRequestDto { Remarks = "Approved" }, "Manager");
            Assert.Equal(DispatchPlanStatus.Approved, plan.Status);

            // Approved -> Ready For Dispatch
            plan = await service.MarkReadyForDispatchAsync(plan.Id, new StatusActionRequestDto { Remarks = "Ready" }, "StoreKeeper");
            Assert.Equal(DispatchPlanStatus.ReadyForDispatch, plan.Status);

            // Ready For Dispatch -> Closed
            plan = await service.CloseDispatchAsync(plan.Id, new StatusActionRequestDto { Remarks = "Closed" }, "Manager");
            Assert.Equal(DispatchPlanStatus.Closed, plan.Status);

            Assert.Equal(5, plan.Timeline.Count);
        }

        [Fact]
        public async Task StatusWorkflow_rejects_invalid_transition()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Acme",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Site 1",
                WarehouseName = "FG Store",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 5 }
                }
            };

            var plan = await service.CreateDispatchPlanAsync(payload, "User");
            Assert.Equal(DispatchPlanStatus.Draft, plan.Status);

            // Attempt Draft -> Approved directly (should fail because must be Planned first)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApproveDispatchAsync(plan.Id, null, "Manager"));
        }

        [Fact]
        public async Task UpdateDispatchPlan_fails_when_plan_is_not_draft()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Acme",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Site 1",
                WarehouseName = "FG Store",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 5 }
                }
            };

            var plan = await service.CreateDispatchPlanAsync(payload, "User");
            await service.PlanDispatchAsync(plan.Id, null, "Planner");

            var updatePayload = new DispatchPlanUpdateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Acme Updated",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Site 1",
                WarehouseName = "FG Store",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 5 }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateDispatchPlanAsync(plan.Id, updatePayload, "User"));
        }

        [Fact]
        public async Task DuplicateDispatchPlan_creates_new_draft_copy()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload = new DispatchPlanCreateRequestDto
            {
                DispatchDate = "2026-08-20",
                CustomerName = "Acme Steel",
                SalesOrderNumber = "SO-101",
                DeliveryAddress = "Pune",
                WarehouseName = "FG Store",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto>
                {
                    new() { FinishedGoodCode = "FG-1", FinishedGoodName = "FG 1", Quantity = 50 }
                }
            };

            var original = await service.CreateDispatchPlanAsync(payload, "User");
            var duplicate = await service.DuplicateDispatchPlanAsync(original.Id, "User");

            Assert.NotEqual(original.Id, duplicate.Id);
            Assert.NotEqual(original.DispatchNumber, duplicate.DispatchNumber);
            Assert.Equal(DispatchPlanStatus.Draft, duplicate.Status);
            Assert.Equal("Acme Steel", duplicate.CustomerName);
            Assert.Single(duplicate.Items);
            Assert.Equal(50, duplicate.Items[0].Quantity);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_counts()
        {
            using var db = CreateDbContext();
            var numbering = new DispatchPlanningNumberingService(db);
            var service = new DispatchPlanningService(db, numbering);

            var payload1 = new DispatchPlanCreateRequestDto
            {
                DispatchDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                CustomerName = "C1",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "A1",
                WarehouseName = "W1",
                PlannedDispatchDate = "2026-08-21",
                ExpectedDeliveryDate = "2026-08-22",
                Items = new List<DispatchPlanItemCreateDto> { new() { FinishedGoodCode = "F1", FinishedGoodName = "N1", Quantity = 1 } }
            };

            var p1 = await service.CreateDispatchPlanAsync(payload1, "User");
            await service.PlanDispatchAsync(p1.Id, null, "User");

            var p2 = await service.CreateDispatchPlanAsync(payload1, "User");
            await service.PlanDispatchAsync(p2.Id, null, "User");
            await service.ApproveDispatchAsync(p2.Id, null, "User");
            await service.MarkReadyForDispatchAsync(p2.Id, null, "User");

            var dashboard = await service.GetDispatchPlanDashboardAsync();

            Assert.Equal(2, dashboard.TodaysDispatches);
            Assert.Equal(1, dashboard.Planned);
            Assert.Equal(1, dashboard.Ready);
            Assert.Equal(0, dashboard.Completed);
        }
    }
}
