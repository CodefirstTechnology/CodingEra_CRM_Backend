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
    public class VehicleAssignmentContractTests
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
                new VehicleAssignmentStatusConverter(),
                new VehicleTypeConverter(),
                new TransportStatusConverter(),
                new TransportModeConverter()
            }
        };

        [Fact]
        public void VehicleAssignmentEnums_serialize_and_deserialize_correctly()
        {
            var statusJson = JsonSerializer.Serialize(VehicleAssignmentStatus.Assigned, _jsonOptions);
            Assert.Equal("\"Assigned\"", statusJson);
            var status = JsonSerializer.Deserialize<VehicleAssignmentStatus>(statusJson, _jsonOptions);
            Assert.Equal(VehicleAssignmentStatus.Assigned, status);

            var typeJson = JsonSerializer.Serialize(VehicleType.Container, _jsonOptions);
            Assert.Equal("\"Container\"", typeJson);
            var vType = JsonSerializer.Deserialize<VehicleType>(typeJson, _jsonOptions);
            Assert.Equal(VehicleType.Container, vType);

            var transportStatusJson = JsonSerializer.Serialize(TransportStatus.InTransit, _jsonOptions);
            Assert.Equal("\"In Transit\"", transportStatusJson);
            var tStatus = JsonSerializer.Deserialize<TransportStatus>(transportStatusJson, _jsonOptions);
            Assert.Equal(TransportStatus.InTransit, tStatus);
        }

        [Fact]
        public async Task CreateVehicleAssignment_creates_record_with_numbering_and_timeline()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchId = 1,
                DispatchNumber = "DN-2026-0001",
                VehicleId = 10,
                VehicleName = "Tata 407",
                VehicleNumber = "MH-12-AB-1234",
                VehicleType = VehicleType.Truck,
                DriverName = "Rajesh Sharma",
                DriverContact = "+91 9876543210",
                TransportCompanyId = 5,
                TransportCompanyName = "Speed Logistics Pvt Ltd",
                LoadingDate = "2026-08-21",
                LoadingTime = "10:30",
                ExpectedDeparture = "2026-08-21T14:00:00Z",
                Capacity = 5000m,
                AssignedQuantity = 4200m,
                Remarks = "Handle with care",
                Notes = "Driver has valid commercial license"
            };

            var result = await service.CreateVehicleAssignmentAsync(payload, "TestUser");

            Assert.NotNull(result);
            Assert.StartsWith("VA-", result.AssignmentNumber);
            Assert.Equal(VehicleAssignmentStatus.Draft, result.Status);
            Assert.Equal("MH-12-AB-1234", result.VehicleNumber);
            Assert.Equal("Rajesh Sharma", result.DriverName);
            Assert.Equal(4200m, result.AssignedQuantity);
            Assert.Single(result.Timeline);
            Assert.Equal("Vehicle assignment created", result.Timeline[0].Action);
        }

        [Theory]
        [InlineData(0, 100, "Capacity must be greater than 0")]
        [InlineData(100, 0, "Assigned quantity must be greater than 0")]
        [InlineData(100, 150, "Assigned quantity cannot exceed vehicle capacity")]
        public async Task CreateVehicleAssignment_validates_capacities(decimal capacity, decimal assignedQty, string expectedError)
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                DriverContact = "12345",
                TransportCompanyName = "Transporter",
                LoadingDate = "2026-08-21",
                Capacity = capacity,
                AssignedQuantity = assignedQty
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateVehicleAssignmentAsync(payload, "User"));
            Assert.Contains(expectedError, ex.Message);
        }

        [Fact]
        public async Task CancelVehicleAssignment_requires_remarks_and_transitions_to_cancelled()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                DriverContact = "12345",
                TransportCompanyName = "Transporter",
                LoadingDate = "2026-08-21",
                Capacity = 1000,
                AssignedQuantity = 800
            };

            var va = await service.CreateVehicleAssignmentAsync(payload, "User");
            Assert.Equal(VehicleAssignmentStatus.Draft, va.Status);

            // Requires reason
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CancelVehicleAssignmentAsync(va.Id, new StatusActionRequestDto { Remarks = "" }, "User"));

            var cancelled = await service.CancelVehicleAssignmentAsync(va.Id, new StatusActionRequestDto { Remarks = "Vehicle breakdown" }, "User");
            Assert.Equal(VehicleAssignmentStatus.Cancelled, cancelled.Status);
        }

        [Fact]
        public async Task StatusWorkflow_and_invalid_transition_rejection()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver",
                DriverContact = "12345",
                TransportCompanyName = "Transporter",
                LoadingDate = "2026-08-21",
                Capacity = 1000,
                AssignedQuantity = 800
            };

            var va = await service.CreateVehicleAssignmentAsync(payload, "User");

            // Attempt invalid Draft -> Loaded
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MarkLoadedAsync(va.Id, null, "User"));

            // Change to Assigned via dispatch assignment flow
            var plan = new DispatchPlan
            {
                DispatchNumber = "DN-2026-0001",
                CustomerName = "Customer 1",
                SalesOrderNumber = "SO-1",
                DeliveryAddress = "Address 1",
                WarehouseName = "Warehouse 1"
            };
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var assignedVa = await service.AssignVehicleToDispatchAsync(plan.Id, payload, "User");
            Assert.Equal(VehicleAssignmentStatus.Assigned, assignedVa.Status);

            // Assigned -> Loaded
            var loaded = await service.MarkLoadedAsync(assignedVa.Id, new StatusActionRequestDto { Remarks = "Loaded" }, "Operator");
            Assert.Equal(VehicleAssignmentStatus.Loaded, loaded.Status);

            // Loaded -> Dispatched
            var dispatched = await service.MarkVehicleDispatchedAsync(assignedVa.Id, new StatusActionRequestDto { Remarks = "Gate out" }, "GateKeeper");
            Assert.Equal(VehicleAssignmentStatus.Dispatched, dispatched.Status);

            // Dispatched -> Completed
            var completed = await service.CompleteVehicleAssignmentAsync(assignedVa.Id, new StatusActionRequestDto { Remarks = "Delivery confirmed" }, "Manager");
            Assert.Equal(VehicleAssignmentStatus.Completed, completed.Status);
        }

        [Fact]
        public async Task ChangeVehicle_updates_vehicle_info_when_allowed()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-12-1234",
                DriverName = "Driver 1",
                DriverContact = "12345",
                TransportCompanyName = "Transporter",
                LoadingDate = "2026-08-21",
                Capacity = 1000,
                AssignedQuantity = 800
            };

            var va = await service.CreateVehicleAssignmentAsync(payload, "User");

            var changePayload = new VehicleAssignmentUpdateRequestDto
            {
                DispatchNumber = "DN-1",
                VehicleNumber = "MH-14-9999",
                DriverName = "Driver 2",
                DriverContact = "99999",
                TransportCompanyName = "Transporter Premium",
                LoadingDate = "2026-08-21",
                Capacity = 1200,
                AssignedQuantity = 800,
                Remarks = "Replacement vehicle"
            };

            var updated = await service.ChangeVehicleAsync(va.Id, changePayload, "User");
            Assert.Equal("MH-14-9999", updated.VehicleNumber);
            Assert.Equal("Driver 2", updated.DriverName);
            Assert.Equal(2, updated.Timeline.Count);
        }

        [Fact]
        public async Task GenerateTransport_creates_linked_transport_detail()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var plan = new DispatchPlan
            {
                DispatchNumber = "DN-2026-0005",
                CustomerName = "Steel Corp",
                SalesOrderNumber = "SO-5",
                DeliveryAddress = "Mumbai Port, Yard 4",
                WarehouseName = "Central Plant Warehouse"
            };
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var payload = new VehicleAssignmentCreateRequestDto
            {
                DispatchId = plan.Id,
                DispatchNumber = plan.DispatchNumber,
                VehicleNumber = "MH-04-TR-5050",
                DriverName = "Amit Kumar",
                DriverContact = "9822012345",
                TransportCompanyName = "Fast Freight",
                LoadingDate = "2026-08-21",
                Capacity = 20000,
                AssignedQuantity = 18500
            };

            var va = await service.CreateVehicleAssignmentAsync(payload, "User");
            var transport = await service.GenerateTransportAsync(va.Id, "User");

            Assert.NotNull(transport);
            Assert.StartsWith("TR-", transport.TransportNumber);
            Assert.Equal(va.Id, transport.VehicleAssignmentId);
            Assert.Equal(plan.Id, transport.DispatchId);
            Assert.Equal("Central Plant Warehouse → Steel Corp", transport.Route);

            // Verify linked ID in DB
            var updatedVa = await db.VehicleAssignments.FindAsync(va.Id);
            Assert.Equal(transport.Id, updatedVa!.TransportId);

            var updatedPlan = await db.DispatchPlans.FindAsync(plan.Id);
            Assert.Equal(transport.Id, updatedPlan!.TransportId);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_counts()
        {
            using var db = CreateDbContext();
            var vaNumbering = new VehicleAssignmentNumberingService(db);
            var trNumbering = new TransportNumberingService(db);
            var service = new VehicleAssignmentService(db, vaNumbering, trNumbering);

            var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var va1 = await service.CreateVehicleAssignmentAsync(new VehicleAssignmentCreateRequestDto
            {
                VehicleNumber = "V1",
                DriverName = "D1",
                DriverContact = "C1",
                TransportCompanyName = "T1",
                LoadingDate = todayStr,
                Capacity = 100,
                AssignedQuantity = 50
            }, "User"); // Draft

            var plan = new DispatchPlan { DispatchNumber = "DN-1", CustomerName = "C", SalesOrderNumber = "SO", DeliveryAddress = "A", WarehouseName = "W" };
            db.DispatchPlans.Add(plan);
            await db.SaveChangesAsync();

            var va2 = await service.AssignVehicleToDispatchAsync(plan.Id, new VehicleAssignmentCreateRequestDto
            {
                VehicleNumber = "V2",
                DriverName = "D2",
                DriverContact = "C2",
                TransportCompanyName = "T2",
                LoadingDate = todayStr,
                Capacity = 100,
                AssignedQuantity = 50
            }, "User"); // Assigned

            var dashboard = await service.GetVehicleAssignmentDashboardAsync();

            Assert.Equal(1, dashboard.VehiclesAssigned); // 1 Assigned
            Assert.Equal(2, dashboard.LoadingToday); // 2 Loading today
            Assert.Equal(1, dashboard.PendingAssignment); // 1 Draft
            Assert.Equal(0, dashboard.CompletedDeliveries);
        }
    }
}
