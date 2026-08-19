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
    }
}
