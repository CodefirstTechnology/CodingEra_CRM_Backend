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
    }
}
