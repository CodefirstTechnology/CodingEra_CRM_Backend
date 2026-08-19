using System;
using System.Collections.Generic;
using ERP.Application.Production.Dtos;
using Xunit;

namespace Backend_ERP.Tests
{
    public class ProductionContractTests
    {
        [Fact]
        public void BomCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new BomCreateRequestDto
            {
                ProductId = 1,
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                Revision = "Rev 1",
                EffectiveDate = "2026-08-18",
                ExpiryDate = "2027-08-18",
                ProcessNotes = "Assembly instructions",
                Notes = "General notes",
                Materials = new List<BomMaterialLineDto>
                {
                    new BomMaterialLineDto
                    {
                        MaterialId = 10,
                        MaterialCode = "MAT-001",
                        MaterialName = "Raw Material A",
                        Quantity = 5.5m,
                        Uom = "KG",
                        WastagePercent = 2.5m,
                        WarehouseId = 2,
                        WarehouseName = "Secondary Store"
                    }
                }
            };

            Assert.Equal("PRD-001", req.ProductCode);
            Assert.Equal("Finished Product A", req.ProductName);
            Assert.Equal("Rev 1", req.Revision);
            Assert.Single(req.Materials);
            Assert.Equal("MAT-001", req.Materials[0].MaterialCode);
            Assert.Equal(5.5m, req.Materials[0].Quantity);
        }

        [Fact]
        public void PlanCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new PlanCreateRequestDto
            {
                PlanningPeriod = "Aug 2026",
                ProductId = 1,
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                BomId = 5,
                RequiredQuantity = 100m,
                PlannedQuantity = 100m,
                WarehouseId = 1,
                WarehouseName = "Main Store",
                Priority = "High",
                Planner = "John Doe",
                ExpectedStart = "2026-08-18",
                ExpectedFinish = "2026-08-25",
                Notes = "Plan notes"
            };

            Assert.Equal("Aug 2026", req.PlanningPeriod);
            Assert.Equal("PRD-001", req.ProductCode);
            Assert.Equal(5, req.BomId);
            Assert.Equal(100m, req.PlannedQuantity);
            Assert.Equal("High", req.Priority);
            Assert.Equal("John Doe", req.Planner);
        }

        [Fact]
        public void WorkOrderCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new WorkOrderCreateRequestDto
            {
                PlanId = 12,
                BomId = 5,
                ProductId = 1,
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                PlannedQuantity = 50m,
                Priority = "Medium",
                Supervisor = "Jane Smith",
                MachineId = 3,
                AssignedTeam = "Team Alpha",
                StartDate = "2026-08-18",
                DueDate = "2026-08-22",
                Notes = "WO notes"
            };

            Assert.Equal(12, req.PlanId);
            Assert.Equal(50m, req.PlannedQuantity);
            Assert.Equal("Medium", req.Priority);
            Assert.Equal("Team Alpha", req.AssignedTeam);
            Assert.Equal(3, req.MachineId);
        }

        [Fact]
        public void MachineListItemDto_exposes_expected_contract_properties()
        {
            var dto = new MachineListItemDto
            {
                Id = 1,
                MachineCode = "MCH-001",
                MachineName = "Machine 1",
                Department = "Assembly",
                RunningHours = 100m,
                IdleHours = 10m,
                BreakdownHours = 5m,
                UtilizationPercent = 90m,
                MaintenanceDue = "2026-08-18",
                Status = "Running",
                AssignedWorkOrderCount = 2
            };

            Assert.Equal("MCH-001", dto.MachineCode);
            Assert.Equal("Running", dto.Status);
            Assert.Equal(2, dto.AssignedWorkOrderCount);
        }

        [Fact]
        public void ScheduleListItemDto_exposes_expected_contract_properties()
        {
            var dto = new ScheduleListItemDto
            {
                Id = 5,
                ScheduleNumber = "SCH-001",
                WorkOrderNumber = "WO-001",
                ProductName = "Product A",
                MachineCode = "MCH-001",
                MachineName = "Machine 1",
                Shift = "A",
                Operator = "Operator A",
                StartTime = "2026-08-18T08:00:00Z",
                UtilizationPercent = 95m,
                Status = "Running"
            };

            Assert.Equal("SCH-001", dto.ScheduleNumber);
            Assert.Equal("A", dto.Shift);
            Assert.Equal("Running", dto.Status);
        }

        [Fact]
        public void EntryCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new EntryCreateRequestDto
            {
                WorkOrderId = 1,
                MachineId = 2,
                ProductionDate = "2026-08-19",
                Shift = "A",
                Operator = "Operator A",
                ProducedQuantity = 10m,
                GoodQuantity = 8m,
                RejectedQuantity = 2m,
                Notes = "Test Notes"
            };

            Assert.Equal(1, req.WorkOrderId);
            Assert.Equal("A", req.Shift);
            Assert.Equal(10m, req.ProducedQuantity);
            Assert.Equal("Test Notes", req.Notes);
        }

        [Fact]
        public void EntryDto_exposes_expected_contract_properties()
        {
            var dto = new EntryDto
            {
                Id = 1,
                EntryNumber = "ENT-001",
                WorkOrderId = 2,
                WorkOrderNumber = "WO-002",
                ProductId = 3,
                ProductCode = "PRD-003",
                ProductName = "Product B",
                ProducedQuantity = 100m,
                GoodQuantity = 95m,
                RejectedQuantity = 5m,
                Shift = "B",
                Operator = "Operator B",
                MachineId = 4,
                MachineCode = "MCH-004",
                MachineName = "Machine 4",
                ProductionDate = "2026-08-19",
                Status = "Draft",
                Notes = "Notes",
                Attachments = new List<ProductionAttachmentDto>(),
                Timeline = new List<ProductionTimelineEventDto>(),
                CreatedBy = "Admin",
                CreatedAt = "2026-08-19T11:00:00Z",
                UpdatedBy = "Admin",
                UpdatedAt = "2026-08-19T11:00:00Z"
            };

            Assert.Equal("ENT-001", dto.EntryNumber);
            Assert.Equal("Draft", dto.Status);
            Assert.Equal(95m, dto.GoodQuantity);
        }

        [Fact]
        public void ConsumptionDto_exposes_expected_contract_properties()
        {
            var dto = new ConsumptionDto
            {
                Id = 1,
                ConsumptionNumber = "MC-001",
                WorkOrderId = 2,
                WorkOrderNumber = "WO-002",
                BomId = 3,
                BomNumber = "BOM-003",
                EntryId = 4,
                MaterialId = 5,
                MaterialCode = "MAT-005",
                MaterialName = "Material C",
                PlannedQuantity = 50m,
                ActualQuantity = 52m,
                Variance = 2m,
                Uom = "KG",
                WarehouseId = 6,
                WarehouseName = "Warehouse 6",
                BatchNumber = "BATCH-001",
                StockOutReference = "STX-001",
                Notes = "Consumption Notes",
                Timeline = new List<ProductionTimelineEventDto>(),
                CreatedBy = "Admin",
                CreatedAt = "2026-08-19T11:00:00Z",
                UpdatedBy = "Admin",
                UpdatedAt = "2026-08-19T11:00:00Z"
            };

            Assert.Equal("MC-001", dto.ConsumptionNumber);
            Assert.Equal("STX-001", dto.StockOutReference);
            Assert.Equal(2m, dto.Variance);
        }

        [Fact]
        public void ReportDashboardDto_exposes_expected_contract_properties()
        {
            var dto = new ReportDashboardDto
            {
                TodaysProduced = 100m,
                WeekProduced = 500m,
                MonthProduced = 2000m,
                RejectionRate = 2.5m,
                AverageUtilization = 85.5m,
                PendingWorkOrders = 5
            };

            Assert.Equal(100m, dto.TodaysProduced);
            Assert.Equal(500m, dto.WeekProduced);
            Assert.Equal(2000m, dto.MonthProduced);
            Assert.Equal(2.5m, dto.RejectionRate);
            Assert.Equal(85.5m, dto.AverageUtilization);
            Assert.Equal(5, dto.PendingWorkOrders);
        }

        [Fact]
        public void DailyReportDto_exposes_expected_contract_properties()
        {
            var dto = new DailyReportDto
            {
                Date = "2026-08-19",
                Shift = "A",
                ProductionSummary = new DailyProductionSummaryDto
                {
                    TotalProduced = 100m,
                    GoodQuantity = 98m,
                    RejectedQuantity = 2m,
                    Productivity = 98m
                },
                ShiftSummary = new List<DailyShiftSummaryDto>(),
                MachineSummary = new List<DailyMachineSummaryDto>(),
                ProductSummary = new List<DailyProductSummaryDto>(),
                MaterialConsumed = 200m,
                Utilization = 80m,
                PendingWorkOrders = 3
            };

            Assert.Equal("2026-08-19", dto.Date);
            Assert.Equal("A", dto.Shift);
            Assert.Equal(98m, dto.ProductionSummary.GoodQuantity);
            Assert.Empty(dto.ShiftSummary);
            Assert.Equal(200m, dto.MaterialConsumed);
        }

        [Fact]
        public void MonthlySummaryDto_exposes_expected_contract_properties()
        {
            var dto = new MonthlySummaryDto
            {
                Month = "2026-08",
                TotalProduced = 5000m,
                GoodQuantity = 4900m,
                RejectedQuantity = 100m,
                MaterialConsumed = 10000m,
                AverageUtilization = 82m,
                WorkOrdersCompleted = 15,
                RejectionRate = 2m,
                DailyTrend = new List<DailyTrendItemDto>()
            };

            Assert.Equal("2026-08", dto.Month);
            Assert.Equal(5000m, dto.TotalProduced);
            Assert.Equal(15, dto.WorkOrdersCompleted);
            Assert.Empty(dto.DailyTrend);
        }

        [Fact]
        public async Task Production_GetPermissions_returns_expected_RBAC_list()
        {
            var service = new ERP.Infrastructure.Production.ProductionService(null!, null!);
            var perms = await service.GetPermissionsAsync();

            Assert.Contains("production.view", perms);
            Assert.Contains("production.create", perms);
            Assert.Contains("production.approve", perms);
            Assert.Contains("production.report.view", perms);
            Assert.Contains("production.dashboard.view", perms);
        }

        [Fact]
        public void RejectionListItemDto_exposes_expected_contract_properties()
        {
            var dto = new RejectionListItemDto
            {
                Id = 1,
                RejectionNumber = "REJ-001",
                WorkOrderNumber = "WO-001",
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                Quantity = 10m,
                Reason = "Scratches",
                Category = "Cosmetic",
                Operator = "Operator A",
                MachineName = "Machine A",
                RejectionDate = "2026-08-18"
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("REJ-001", dto.RejectionNumber);
            Assert.Equal("WO-001", dto.WorkOrderNumber);
            Assert.Equal(10m, dto.Quantity);
            Assert.Equal("Scratches", dto.Reason);
        }

        [Fact]
        public void RejectionDto_exposes_expected_contract_properties()
        {
            var dto = new RejectionDto
            {
                Id = 1,
                RejectionNumber = "REJ-001",
                WorkOrderId = 2,
                WorkOrderNumber = "WO-001",
                EntryId = 3,
                ProductId = 4,
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                Quantity = 10m,
                Reason = "Scratches",
                Category = "Cosmetic",
                Operator = "Operator A",
                MachineId = 5,
                MachineCode = "MAC-001",
                MachineName = "Machine A",
                RejectionDate = "2026-08-18",
                CorrectiveAction = "Adjust speed",
                Notes = "Rejection notes",
                Timeline = new List<ProductionTimelineEventDto>(),
                CreatedBy = "user",
                CreatedAt = "2026-08-18T10:00:00Z",
                UpdatedBy = "user",
                UpdatedAt = "2026-08-18T10:00:00Z"
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("REJ-001", dto.RejectionNumber);
            Assert.Equal(2, dto.WorkOrderId);
            Assert.Equal(3, dto.EntryId);
            Assert.Equal(10m, dto.Quantity);
            Assert.Equal("Adjust speed", dto.CorrectiveAction);
        }

        [Fact]
        public void RejectionDashboardDto_exposes_expected_contract_properties()
        {
            var dto = new RejectionDashboardDto
            {
                TotalRejections = 5,
                RejectionRate = 1.5m,
                TopReasons = new List<RejectionReasonCountDto>
                {
                    new RejectionReasonCountDto { Reason = "Scratches", Count = 3 }
                }
            };

            Assert.Equal(5, dto.TotalRejections);
            Assert.Equal(1.5m, dto.RejectionRate);
            Assert.Single(dto.TopReasons);
            Assert.Equal("Scratches", dto.TopReasons[0].Reason);
            Assert.Equal(3, dto.TopReasons[0].Count);
        }
    }
}
