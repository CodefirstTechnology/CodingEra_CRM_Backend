using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class QualityControlContractTests
    {
        [Fact]
        public void QualityControlRules_validates_incoming_quantities()
        {
            var err = QualityControlRules.ValidateIncomingQuantities(100m, 90m, 10m, 0m);
            Assert.Null(err);

            var invalidErr = QualityControlRules.ValidateIncomingQuantities(100m, 90m, 20m, 0m);
            Assert.NotNull(invalidErr);
            Assert.Contains("cannot exceed sampling quantity", invalidErr);
        }

        [Theory]
        [InlineData(IncomingInspectionStatus.Draft, IncomingInspectionStatus.Submitted, true)]
        [InlineData(IncomingInspectionStatus.Submitted, IncomingInspectionStatus.Approved, true)]
        [InlineData(IncomingInspectionStatus.Approved, IncomingInspectionStatus.Closed, true)]
        [InlineData(IncomingInspectionStatus.Closed, IncomingInspectionStatus.Approved, false)]
        public void QualityControlRules_validates_incoming_status_transitions(IncomingInspectionStatus current, IncomingInspectionStatus target, bool expected)
        {
            Assert.Equal(expected, QualityControlRules.CanTransitionIncoming(current, target));
        }

        [Fact]
        public void IncomingDto_exposes_expected_json_contract_properties()
        {
            var dto = new IncomingDto
            {
                Id = 1,
                InspectionNumber = "INSP-2026-000001",
                VendorName = "Apex Raw Materials",
                PurchaseOrderNumber = "PO-2026-000002",
                GRNNumber = "GRN-2026-000001",
                MaterialName = "Steel Rod 20mm",
                SamplingQuantity = 100m,
                AcceptedQuantity = 95m,
                RejectedQuantity = 5m,
                InspectionResult = InspectionResult.Accepted,
                Status = IncomingInspectionStatus.Approved
            };

            Assert.Equal("INSP-2026-000001", dto.InspectionNumber);
            Assert.Equal("Apex Raw Materials", dto.VendorName);
            Assert.Equal("PO-2026-000002", dto.PurchaseOrderNumber);
            Assert.Equal("GRN-2026-000001", dto.GRNNumber);
            Assert.Equal("Steel Rod 20mm", dto.MaterialName);
            Assert.Equal(InspectionResult.Accepted, dto.InspectionResult);
            Assert.Equal(IncomingInspectionStatus.Approved, dto.Status);
        }

        [Fact]
        public void InProcessDto_exposes_expected_json_contract_properties()
        {
            var dto = new InProcessDto
            {
                Id = 1,
                QCNumber = "QC-2026-000001",
                WorkOrderNumber = "WO-2026-0010",
                MachineName = "CNC Lathe Machine #1",
                Operator = "John Doe",
                Shift = Shift.A,
                Result = QcCheckResult.Pass,
                Status = InProcessQcStatus.Passed
            };

            Assert.Equal("QC-2026-000001", dto.QCNumber);
            Assert.Equal("CNC Lathe Machine #1", dto.MachineName);
            Assert.Equal(Shift.A, dto.Shift);
            Assert.Equal(QcCheckResult.Pass, dto.Result);
            Assert.Equal(InProcessQcStatus.Passed, dto.Status);
        }

        [Fact]
        public void RejectionDto_exposes_expected_json_contract_properties()
        {
            var dto = new RejectionDto
            {
                Id = 1,
                RejectionNumber = "REJ-2026-000001",
                Source = RejectionSource.Incoming,
                SourceRecordNumber = "INSP-2026-000001",
                Quantity = 10m,
                Reason = "Dimensional Tolerance Violation",
                Department = "Quality Assurance",
                Status = RejectionAnalysisStatus.Open
            };

            Assert.Equal("REJ-2026-000001", dto.RejectionNumber);
            Assert.Equal(RejectionSource.Incoming, dto.Source);
            Assert.Equal(10m, dto.Quantity);
            Assert.Equal("Dimensional Tolerance Violation", dto.Reason);
            Assert.Equal(RejectionAnalysisStatus.Open, dto.Status);
        }
    }
}
