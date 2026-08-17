using System;
using System.Collections.Generic;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PurchaseRequisitionContractTests
    {
        [Fact]
        public void PurchaseRequisitionRules_validates_required_fields_and_lines()
        {
            Assert.Throws<InvalidOperationException>(() =>
                PurchaseRequisitionRules.ValidateCreate("", "User1", DateTime.UtcNow, new List<(string, decimal, decimal)> { ("Item 1", 10m, 100m) }));

            Assert.Throws<InvalidOperationException>(() =>
                PurchaseRequisitionRules.ValidateCreate("Manufacturing", "", DateTime.UtcNow, new List<(string, decimal, decimal)> { ("Item 1", 10m, 100m) }));

            Assert.Throws<InvalidOperationException>(() =>
                PurchaseRequisitionRules.ValidateCreate("Manufacturing", "User1", DateTime.UtcNow, new List<(string, decimal, decimal)>()));
        }

        [Theory]
        [InlineData(PurchaseRequisitionStatus.Draft, PurchaseRequisitionStatus.Submitted, true)]
        [InlineData(PurchaseRequisitionStatus.Submitted, PurchaseRequisitionStatus.Approved, true)]
        [InlineData(PurchaseRequisitionStatus.Submitted, PurchaseRequisitionStatus.Rejected, true)]
        [InlineData(PurchaseRequisitionStatus.Approved, PurchaseRequisitionStatus.ConvertedToRFQ, true)]
        [InlineData(PurchaseRequisitionStatus.Closed, PurchaseRequisitionStatus.Approved, false)]
        public void PurchaseRequisitionRules_validates_status_transitions(PurchaseRequisitionStatus current, PurchaseRequisitionStatus target, bool expected)
        {
            Assert.Equal(expected, PurchaseRequisitionRules.CanTransition(current, target));
        }

        [Fact]
        public void PurchaseRequisitionCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new PurchaseRequisitionCreateRequestDto
            {
                Department = "Maintenance",
                Requestor = "John Doe",
                Priority = PurchaseRequisitionPriority.High,
                Lines = new List<PurchaseRequisitionLineDto>
                {
                    new PurchaseRequisitionLineDto
                    {
                        ItemName = "Hydraulic Pump Motor",
                        Quantity = 2,
                        Uom = "PCS",
                        EstimatedPrice = 15000m,
                        TotalAmount = 30000m
                    }
                }
            };

            Assert.Equal("Maintenance", req.Department);
            Assert.Equal("John Doe", req.Requestor);
            Assert.Equal(PurchaseRequisitionPriority.High, req.Priority);
            Assert.Single(req.Lines);
            Assert.Equal("Hydraulic Pump Motor", req.Lines[0].ItemName);
            Assert.Equal(30000m, req.Lines[0].TotalAmount);
        }
    }
}
