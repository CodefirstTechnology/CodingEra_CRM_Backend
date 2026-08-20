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
    public class EwayBillContractTests
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
                new EwayStatusConverter()
            }
        };

        [Fact]
        public void EwayStatus_serializes_and_deserializes_correctly()
        {
            var activeJson = JsonSerializer.Serialize(EwayStatus.Active, _jsonOptions);
            Assert.Equal("\"Active\"", activeJson);
            var active = JsonSerializer.Deserialize<EwayStatus>(activeJson, _jsonOptions);
            Assert.Equal(EwayStatus.Active, active);

            var expiredJson = JsonSerializer.Serialize(EwayStatus.Expired, _jsonOptions);
            Assert.Equal("\"Expired\"", expiredJson);
            var expired = JsonSerializer.Deserialize<EwayStatus>(expiredJson, _jsonOptions);
            Assert.Equal(EwayStatus.Expired, expired);
        }

        [Fact]
        public async Task CreateEwayBill_creates_record_with_numbering_and_timeline()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchId = 1,
                DispatchNumber = "DN-2026-0001",
                InvoiceNumber = "INV-2026-0145",
                CustomerId = 10,
                CustomerName = "Apex Heavy Engineering",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-PQ-5678",
                TransportId = 2,
                TransportNumber = "TR-2026-0002",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 145.5m,
                Remarks = "Heavy cargo with transit insurance",
                Notes = "NIC portal sync validated"
            };

            var result = await service.CreateEwayBillAsync(payload, "TestUser");

            Assert.NotNull(result);
            Assert.StartsWith("EWB-", result.EwayBillNumber);
            Assert.Equal(EwayStatus.Draft, result.Status);
            Assert.Equal("Apex Heavy Engineering", result.CustomerName);
            Assert.Equal(145.5m, result.DistanceKm);
            Assert.Single(result.Timeline);
            Assert.Equal("E-Way bill created", result.Timeline[0].Action);
        }

        [Theory]
        [InlineData("2026-08-25", "2026-08-20", 50, "Validity From must be on or before Validity To")]
        [InlineData("2026-08-20", "2026-08-22", 0, "Distance must be greater than 0")]
        [InlineData("2026-08-20", "2026-08-22", -5, "Distance must be greater than 0")]
        public async Task CreateEwayBill_validates_dates_and_distance(string validFrom, string validTo, decimal distance, string expectedError)
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchNumber = "DN-1",
                InvoiceNumber = "INV-1",
                CustomerName = "Cust",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = validFrom,
                ValidityTo = validTo,
                DistanceKm = distance
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEwayBillAsync(payload, "User"));
            Assert.Contains(expectedError, ex.Message);
        }

        [Fact]
        public async Task StatusWorkflow_transitions_from_draft_to_generated_active_extended_closed()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchNumber = "DN-1",
                InvoiceNumber = "INV-1",
                CustomerName = "Cust",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 100
            };

            var eway = await service.CreateEwayBillAsync(payload, "User");
            Assert.Equal(EwayStatus.Draft, eway.Status);

            // Draft -> Generated
            eway = await service.GenerateEwayBillAsync(eway.Id, new StatusActionRequestDto { Remarks = "E-Way generated" }, "Manager");
            Assert.Equal(EwayStatus.Generated, eway.Status);

            // Generated -> Active
            eway = await service.ActivateEwayBillAsync(eway.Id, new StatusActionRequestDto { Remarks = "Vehicle departed, active" }, "GateOfficer");
            Assert.Equal(EwayStatus.Active, eway.Status);

            // Extend validity by 7 days
            eway = await service.ExtendValidityAsync(eway.Id, new EwayExtendRequestDto { ValidityTo = "2026-08-29", Remarks = "Route delay due to rain" }, "Transporter");
            Assert.Equal(EwayStatus.Active, eway.Status);
            Assert.Equal("2026-08-29", eway.ValidityTo);

            // Active -> Closed
            eway = await service.CloseEwayBillAsync(eway.Id, new StatusActionRequestDto { Remarks = "Consignment delivered safely" }, "Manager");
            Assert.Equal(EwayStatus.Closed, eway.Status);

            Assert.Equal(5, eway.Timeline.Count);
        }

        [Fact]
        public async Task StatusWorkflow_rejects_invalid_transitions()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchNumber = "DN-1",
                InvoiceNumber = "INV-1",
                CustomerName = "Cust",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 100
            };

            var eway = await service.CreateEwayBillAsync(payload, "User");

            // Attempt invalid Draft -> Active directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ActivateEwayBillAsync(eway.Id, null, "User"));

            // Attempt invalid Draft -> Extend directly
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExtendValidityAsync(eway.Id, null, "User"));
        }

        [Fact]
        public async Task Update_and_Delete_allowed_only_in_draft()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchNumber = "DN-1",
                InvoiceNumber = "INV-1",
                CustomerName = "Cust",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 100
            };

            var eway = await service.CreateEwayBillAsync(payload, "User");
            await service.GenerateEwayBillAsync(eway.Id, null, "User");

            var updatePayload = new EwayUpdateRequestDto
            {
                DispatchNumber = "DN-1",
                InvoiceNumber = "INV-1",
                CustomerName = "Cust Updated",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 150
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateEwayBillAsync(eway.Id, updatePayload, "User"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteEwayBillAsync(eway.Id));
        }

        [Fact]
        public async Task DuplicateEwayBill_for_active_dispatch_is_prevented()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var payload = new EwayCreateRequestDto
            {
                DispatchId = 10,
                DispatchNumber = "DN-2026-0010",
                InvoiceNumber = "INV-10",
                CustomerName = "Cust",
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = "MH-12-1234",
                TransportNumber = "TR-1",
                ValidityFrom = "2026-08-20",
                ValidityTo = "2026-08-22",
                DistanceKm = 100
            };

            await service.CreateEwayBillAsync(payload, "User");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEwayBillAsync(payload, "User"));
            Assert.Contains("already exists for dispatch DN-2026-0010", ex.Message);
        }

        [Fact]
        public async Task DashboardMetrics_calculates_correct_kpis()
        {
            using var db = CreateDbContext();
            var ewayNumbering = new EwayNumberingService(db);
            var service = new EwayBillService(db, ewayNumbering);

            var today = DateTime.UtcNow;

            // 1 Active expiring soon (validityTo = today + 1 day)
            var e1 = await service.CreateEwayBillAsync(new EwayCreateRequestDto
            {
                DispatchNumber = "D1",
                InvoiceNumber = "I1",
                CustomerName = "C1",
                GstNumber = "GST1",
                VehicleNumber = "V1",
                TransportNumber = "T1",
                ValidityFrom = today.ToString("yyyy-MM-dd"),
                ValidityTo = today.AddDays(1).ToString("yyyy-MM-dd"),
                DistanceKm = 50
            }, "User");
            await service.GenerateEwayBillAsync(e1.Id, null, "User");
            await service.ActivateEwayBillAsync(e1.Id, null, "User");

            // 1 Active expiring later (validityTo = today + 10 days)
            var e2 = await service.CreateEwayBillAsync(new EwayCreateRequestDto
            {
                DispatchNumber = "D2",
                InvoiceNumber = "I2",
                CustomerName = "C2",
                GstNumber = "GST2",
                VehicleNumber = "V2",
                TransportNumber = "T2",
                ValidityFrom = today.ToString("yyyy-MM-dd"),
                ValidityTo = today.AddDays(10).ToString("yyyy-MM-dd"),
                DistanceKm = 500
            }, "User");
            await service.GenerateEwayBillAsync(e2.Id, null, "User");
            await service.ActivateEwayBillAsync(e2.Id, null, "User");

            var dashboard = await service.GetEwayDashboardAsync();

            Assert.Equal(2, dashboard.ActiveBills);
            Assert.Equal(1, dashboard.ExpiringSoon);
            Assert.Equal(0, dashboard.Expired);
        }
    }
}
