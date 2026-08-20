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
    public class DispatchStatusContractTests
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
                new DispatchTrackStatusConverter(),
                new TransportTrackStatusConverter()
            }
        };

        [Fact]
        public void DispatchTrackStatus_serializes_and_deserializes_correctly()
        {
            var inTransitJson = JsonSerializer.Serialize(DispatchTrackStatus.InTransit, _jsonOptions);
            Assert.Equal("\"In Transit\"", inTransitJson);
            var inTransit = JsonSerializer.Deserialize<DispatchTrackStatus>(inTransitJson, _jsonOptions);
            Assert.Equal(DispatchTrackStatus.InTransit, inTransit);

            var delayedJson = JsonSerializer.Serialize(DispatchTrackStatus.Delayed, _jsonOptions);
            Assert.Equal("\"Delayed\"", delayedJson);
            var delayed = JsonSerializer.Deserialize<DispatchTrackStatus>(delayedJson, _jsonOptions);
            Assert.Equal(DispatchTrackStatus.Delayed, delayed);
        }

        [Fact]
        public async Task AutoSync_initializes_tracking_for_existing_dispatch_plans()
        {
            using var db = CreateDbContext();
            var service = new DispatchStatusService(db);

            db.DispatchPlans.Add(new DispatchPlan
            {
                DispatchNumber = "DN-2026-0001",
                CustomerName = "Apex Engineering",
                PlannedDispatchDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var list = await service.GetDispatchStatusesAsync();

            Assert.Single(list);
            Assert.Equal("DN-2026-0001", list[0].DispatchNumber);
            Assert.Equal(DispatchTrackStatus.Ready, list[0].CurrentStatus);
            Assert.Equal(WarehouseTrackStatus.Staged, list[0].WarehouseStatus);
            Assert.Equal(VehicleTrackStatus.Pending, list[0].VehicleStatus);
            Assert.Equal(TransportTrackStatus.Pending, list[0].TransportStatus);
        }

        [Fact]
        public async Task UpdateDispatchStatus_updates_status_actual_delivery_and_records_timeline()
        {
            using var db = CreateDbContext();
            var service = new DispatchStatusService(db);

            var plan = new DispatchPlan
            {
                DispatchNumber = "DN-2026-0002",
                CustomerName = "Tata Motors",
                PlannedDispatchDate = DateTime.UtcNow
            };
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var list = await service.GetDispatchStatusesAsync();
            var trackId = list[0].Id;

            // Advance to Dispatched
            var updated1 = await service.UpdateDispatchStatusAsync(trackId, new DispatchStatusUpdateRequestDto
            {
                CurrentStatus = DispatchTrackStatus.Dispatched,
                WarehouseStatus = WarehouseTrackStatus.Issued,
                VehicleStatus = VehicleTrackStatus.Dispatched,
                Remarks = "Dispatched from plant gate"
            }, "GateOfficer");

            Assert.Equal(DispatchTrackStatus.Dispatched, updated1.CurrentStatus);
            Assert.Equal(WarehouseTrackStatus.Issued, updated1.WarehouseStatus);

            // Advance to In Transit
            var updated2 = await service.UpdateDispatchStatusAsync(trackId, new DispatchStatusUpdateRequestDto
            {
                CurrentStatus = DispatchTrackStatus.InTransit,
                TransportStatus = TransportTrackStatus.InTransit,
                Remarks = "On highway towards destination"
            }, "Driver");

            Assert.Equal(DispatchTrackStatus.InTransit, updated2.CurrentStatus);

            // Mark Delivered
            var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var updated3 = await service.UpdateDispatchStatusAsync(trackId, new DispatchStatusUpdateRequestDto
            {
                CurrentStatus = DispatchTrackStatus.Delivered,
                TransportStatus = TransportTrackStatus.Delivered,
                ActualDelivery = todayStr,
                Remarks = "Consignment received at factory gate"
            }, "Receiver");

            Assert.Equal(DispatchTrackStatus.Delivered, updated3.CurrentStatus);
            Assert.Equal(todayStr, updated3.ActualDelivery);

            // Verify Timeline
            var detail = await service.GetDispatchStatusByIdAsync(trackId);
            Assert.Equal(4, detail.Timeline.Count); // 1 initial + 3 updates
        }

        [Fact]
        public async Task MarkDelayed_sets_delayed_status_delay_hours_and_remarks()
        {
            using var db = CreateDbContext();
            var service = new DispatchStatusService(db);

            var plan = new DispatchPlan
            {
                DispatchNumber = "DN-2026-0003",
                CustomerName = "Bharat Forge",
                PlannedDispatchDate = DateTime.UtcNow
            };
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var list = await service.GetDispatchStatusesAsync();
            var trackId = list[0].Id;

            var updated = await service.UpdateDispatchStatusAsync(trackId, new DispatchStatusUpdateRequestDto
            {
                CurrentStatus = DispatchTrackStatus.Delayed,
                DelayHours = 4,
                Remarks = "Tyre puncture and heavy rainfall near highway toll"
            }, "FleetManager");

            Assert.Equal(DispatchTrackStatus.Delayed, updated.CurrentStatus);
            Assert.Equal(4, updated.DelayHours);
            Assert.Equal("Tyre puncture and heavy rainfall near highway toll", updated.Remarks);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_kpis()
        {
            using var db = CreateDbContext();
            var service = new DispatchStatusService(db);

            db.DispatchPlans.Add(new DispatchPlan { DispatchNumber = "DN-01", PlannedDispatchDate = DateTime.UtcNow });
            db.DispatchPlans.Add(new DispatchPlan { DispatchNumber = "DN-02", PlannedDispatchDate = DateTime.UtcNow });
            db.DispatchPlans.Add(new DispatchPlan { DispatchNumber = "DN-03", PlannedDispatchDate = DateTime.UtcNow });
            db.DispatchPlans.Add(new DispatchPlan { DispatchNumber = "DN-04", PlannedDispatchDate = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var list = await service.GetDispatchStatusesAsync();

            // DN-01 -> Ready (default)
            // DN-02 -> In Transit
            await service.UpdateDispatchStatusAsync(list[1].Id, new DispatchStatusUpdateRequestDto { CurrentStatus = DispatchTrackStatus.InTransit }, "User");
            // DN-03 -> Delivered
            await service.UpdateDispatchStatusAsync(list[2].Id, new DispatchStatusUpdateRequestDto { CurrentStatus = DispatchTrackStatus.Delivered }, "User");
            // DN-04 -> Delayed
            await service.UpdateDispatchStatusAsync(list[3].Id, new DispatchStatusUpdateRequestDto { CurrentStatus = DispatchTrackStatus.Delayed }, "User");

            var dashboard = await service.GetDispatchStatusDashboardAsync();

            Assert.Equal(1, dashboard.Ready);
            Assert.Equal(1, dashboard.InTransit);
            Assert.Equal(1, dashboard.Delivered);
            Assert.Equal(1, dashboard.Delayed);
        }
    }
}
