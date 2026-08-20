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
    public class TransportDetailsContractTests
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
                new TransportStatusConverter(),
                new TransportModeConverter()
            }
        };

        [Fact]
        public void TransportEnums_serialize_and_deserialize_correctly()
        {
            var inTransitJson = JsonSerializer.Serialize(TransportStatus.InTransit, _jsonOptions);
            Assert.Equal("\"In Transit\"", inTransitJson);
            var inTransit = JsonSerializer.Deserialize<TransportStatus>(inTransitJson, _jsonOptions);
            Assert.Equal(TransportStatus.InTransit, inTransit);

            var modeJson = JsonSerializer.Serialize(TransportMode.Multimodal, _jsonOptions);
            Assert.Equal("\"Multimodal\"", modeJson);
            var mode = JsonSerializer.Deserialize<TransportMode>(modeJson, _jsonOptions);
            Assert.Equal(TransportMode.Multimodal, mode);
        }

        [Fact]
        public async Task CreateTransport_creates_record_with_numbering_and_timeline()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var payload = new TransportCreateRequestDto
            {
                DispatchId = 1,
                DispatchNumber = "DN-2026-0001",
                VehicleAssignmentId = 2,
                AssignmentNumber = "VA-2026-0002",
                VehicleNumber = "MH-12-PQ-5678",
                DriverName = "Sunil Patil",
                TransportCompanyName = "Express Roadways",
                Mode = TransportMode.Road,
                Route = "Pune Plant → JNPT Port",
                Source = "Pune Plant Warehouse",
                Destination = "JNPT Container Terminal, Navi Mumbai",
                DistanceKm = 145.5m,
                EstimatedTimeHours = 4.5m,
                FuelNotes = "Full tank diesel filled at Chakan pump",
                Remarks = "Urgent export consignment",
                Notes = "Toll FASTag active"
            };

            var result = await service.CreateTransportAsync(payload, "TestUser");

            Assert.NotNull(result);
            Assert.StartsWith("TR-", result.TransportNumber);
            Assert.Equal(TransportStatus.Draft, result.Status);
            Assert.Equal("Pune Plant → JNPT Port", result.Route);
            Assert.Equal(145.5m, result.DistanceKm);
            Assert.Single(result.Timeline);
            Assert.Equal("Transport created", result.Timeline[0].Action);
        }

        [Theory]
        [InlineData(-10, 5, "Distance cannot be negative")]
        [InlineData(100, -2, "Estimated time cannot be negative")]
        public async Task CreateTransport_validates_distance_and_eta(decimal distance, decimal eta, string expectedError)
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var payload = new TransportCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                TransportCompanyName = "Transporter",
                Route = "A → B",
                Source = "A",
                Destination = "B",
                DistanceKm = distance,
                EstimatedTimeHours = eta
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransportAsync(payload, "User"));
            Assert.Contains(expectedError, ex.Message);
        }

        [Fact]
        public async Task StatusWorkflow_transitions_and_departure_arrival_timestamps()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var payload = new TransportCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                TransportCompanyName = "Transporter",
                Route = "Plant → Site",
                Source = "Plant",
                Destination = "Site",
                DistanceKm = 50,
                EstimatedTimeHours = 2
            };

            var transport = await service.CreateTransportAsync(payload, "User");
            Assert.Equal(TransportStatus.Draft, transport.Status);
            Assert.Null(transport.ActualDeparture);

            // Draft -> In Transit via StartJourney
            transport = await service.StartJourneyAsync(transport.Id, new StatusActionRequestDto { Remarks = "Left warehouse" }, "GateUser");
            Assert.Equal(TransportStatus.InTransit, transport.Status);
            Assert.NotNull(transport.ActualDeparture);
            Assert.Null(transport.ActualArrival);

            // In Transit -> Delivered
            transport = await service.MarkDeliveredAsync(transport.Id, new StatusActionRequestDto { Remarks = "Reached destination" }, "DriverUser");
            Assert.Equal(TransportStatus.Delivered, transport.Status);
            Assert.NotNull(transport.ActualArrival);

            // Delivered -> Closed (requires remarks)
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CloseTransportAsync(transport.Id, new StatusActionRequestDto { Remarks = "" }, "Manager"));

            transport = await service.CloseTransportAsync(transport.Id, new StatusActionRequestDto { Remarks = "Verified POD and closed" }, "Manager");
            Assert.Equal(TransportStatus.Closed, transport.Status);

            Assert.Equal(4, transport.Timeline.Count);
        }

        [Fact]
        public async Task StatusWorkflow_rejects_invalid_transitions()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var payload = new TransportCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                TransportCompanyName = "Transporter",
                Route = "Plant → Site",
                Source = "Plant",
                Destination = "Site",
                DistanceKm = 50,
                EstimatedTimeHours = 2
            };

            var transport = await service.CreateTransportAsync(payload, "User");

            // Attempt invalid Draft -> Delivered directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MarkDeliveredAsync(transport.Id, null, "User"));
        }

        [Fact]
        public async Task Update_and_Delete_allowed_only_in_draft()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var payload = new TransportCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                TransportCompanyName = "Transporter",
                Route = "Plant → Site",
                Source = "Plant",
                Destination = "Site",
                DistanceKm = 50,
                EstimatedTimeHours = 2
            };

            var transport = await service.CreateTransportAsync(payload, "User");
            await service.StartJourneyAsync(transport.Id, null, "User");

            var updatePayload = new TransportUpdateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                TransportCompanyName = "Transporter",
                Route = "Plant → Site Updated",
                Source = "Plant",
                Destination = "Site",
                DistanceKm = 60,
                EstimatedTimeHours = 2
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateTransportAsync(transport.Id, updatePayload, "User"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteTransportAsync(transport.Id));
        }

        [Fact]
        public async Task GenerateLR_creates_linked_lr_with_correct_data()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var plan = new DispatchPlan
            {
                DispatchNumber = "DN-2026-0009",
                CustomerId = 10,
                CustomerName = "Apex Heavy Engineering",
                SalesOrderNumber = "SO-9",
                DeliveryAddress = "Mumbai Works",
                WarehouseName = "Plant WH"
            };
            plan.Items.Add(new DispatchPlanItem
            {
                FinishedGoodCode = "FG-001",
                FinishedGoodName = "Steel Flange",
                BatchNumber = "B-1",
                Quantity = 100,
                Uom = "NOS",
                WarehouseName = "Plant WH"
            });
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var payload = new TransportCreateRequestDto
            {
                DispatchId = plan.Id,
                DispatchNumber = plan.DispatchNumber,
                VehicleNumber = "MH-01-AX-7788",
                DriverName = "Vikas Patil",
                TransportCompanyName = "Apex Transport",
                Route = "Plant WH → Mumbai Works",
                Source = "Plant WH",
                Destination = "Mumbai Works",
                DistanceKm = 120,
                EstimatedTimeHours = 3
            };

            var transport = await service.CreateTransportAsync(payload, "User");
            var lr = await service.GenerateLrAsync(transport.Id, "User");

            Assert.NotNull(lr);
            Assert.StartsWith("LR-", lr.LrNumber);
            Assert.Equal(transport.Id, lr.TransportId);
            Assert.Equal("Apex Heavy Engineering", lr.CustomerName);
            Assert.Equal("Apex Heavy Engineering", lr.Consignee);
            Assert.Equal(1, lr.Packages);
            Assert.Equal(500m, lr.WeightKg); // 100 * 5
            Assert.Equal(LrStatus.Draft, lr.Status);

            // Repeated call returns existing
            var lr2 = await service.GenerateLrAsync(transport.Id, "User");
            Assert.Equal(lr.Id, lr2.Id);
            Assert.Equal(lr.LrNumber, lr2.LrNumber);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_kpis()
        {
            using var db = CreateDbContext();
            var trNumbering = new TransportNumberingService(db);
            var lrNumbering = new LrNumberingService(db);
            var service = new TransportDetailsService(db, trNumbering, lrNumbering);

            var t1 = await service.CreateTransportAsync(new TransportCreateRequestDto
            {
                DispatchNumber = "D1",
                VehicleNumber = "V1",
                DriverName = "Dr1",
                TransportCompanyName = "TC1",
                Route = "R1",
                Source = "S1",
                Destination = "D1",
                DistanceKm = 10,
                EstimatedTimeHours = 1
            }, "User");
            await service.StartJourneyAsync(t1.Id, null, "User"); // In Transit

            var t2 = await service.CreateTransportAsync(new TransportCreateRequestDto
            {
                DispatchNumber = "D2",
                VehicleNumber = "V2",
                DriverName = "Dr2",
                TransportCompanyName = "TC2",
                Route = "R2",
                Source = "S2",
                Destination = "D2",
                DistanceKm = 10,
                EstimatedTimeHours = 1
            }, "User");
            await service.StartJourneyAsync(t2.Id, null, "User");
            await service.MarkDeliveredAsync(t2.Id, null, "User"); // Delivered Today

            var dashboard = await service.GetTransportDashboardAsync();

            Assert.Equal(1, dashboard.VehiclesInTransit);
            Assert.Equal(1, dashboard.DeliveredToday);
            Assert.Equal(0, dashboard.DelayedShipments);
        }
    }
}
