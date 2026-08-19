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
        [InlineData(IncomingInspectionStatus.Draft, IncomingInspectionStatus.Approved, false)]
        [InlineData(IncomingInspectionStatus.Submitted, IncomingInspectionStatus.Approved, true)]
        [InlineData(IncomingInspectionStatus.Submitted, IncomingInspectionStatus.Rejected, true)]
        [InlineData(IncomingInspectionStatus.Approved, IncomingInspectionStatus.Closed, true)]
        [InlineData(IncomingInspectionStatus.Rejected, IncomingInspectionStatus.Closed, true)]
        [InlineData(IncomingInspectionStatus.Rejected, IncomingInspectionStatus.Approved, false)]
        [InlineData(IncomingInspectionStatus.Closed, IncomingInspectionStatus.Approved, false)]
        public void QualityControlRules_validates_incoming_status_transitions(IncomingInspectionStatus current, IncomingInspectionStatus target, bool expected)
        {
            Assert.Equal(expected, QualityControlRules.CanTransitionIncoming(current, target));
        }

        [Theory]
        [InlineData(InProcessQcStatus.Draft, InProcessQcStatus.Running, true)]
        [InlineData(InProcessQcStatus.Draft, InProcessQcStatus.Passed, false)]
        [InlineData(InProcessQcStatus.Draft, InProcessQcStatus.Failed, false)]
        [InlineData(InProcessQcStatus.Running, InProcessQcStatus.Passed, true)]
        [InlineData(InProcessQcStatus.Running, InProcessQcStatus.Failed, true)]
        [InlineData(InProcessQcStatus.Passed, InProcessQcStatus.Closed, true)]
        [InlineData(InProcessQcStatus.Failed, InProcessQcStatus.Closed, true)]
        [InlineData(InProcessQcStatus.Failed, InProcessQcStatus.Passed, false)]
        [InlineData(InProcessQcStatus.Closed, InProcessQcStatus.Running, false)]
        public void QualityControlRules_validates_in_process_status_transitions(InProcessQcStatus current, InProcessQcStatus target, bool expected)
        {
            Assert.Equal(expected, QualityControlRules.CanTransitionInProcess(current, target));
        }

        [Theory]
        [InlineData(FinalInspectionStatus.Draft, FinalInspectionStatus.Submitted, true)]
        [InlineData(FinalInspectionStatus.Draft, FinalInspectionStatus.Approved, false)]
        [InlineData(FinalInspectionStatus.Draft, FinalInspectionStatus.Rejected, false)]
        [InlineData(FinalInspectionStatus.Submitted, FinalInspectionStatus.Approved, true)]
        [InlineData(FinalInspectionStatus.Submitted, FinalInspectionStatus.Rejected, true)]
        [InlineData(FinalInspectionStatus.Approved, FinalInspectionStatus.Closed, true)]
        [InlineData(FinalInspectionStatus.Rejected, FinalInspectionStatus.Closed, true)]
        [InlineData(FinalInspectionStatus.Rejected, FinalInspectionStatus.Approved, false)]
        [InlineData(FinalInspectionStatus.Closed, FinalInspectionStatus.Submitted, false)]
        public void QualityControlRules_validates_final_status_transitions(FinalInspectionStatus current, FinalInspectionStatus target, bool expected)
        {
            Assert.Equal(expected, QualityControlRules.CanTransitionFinal(current, target));
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

        [Fact]
        public void InProcessCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new InProcessCreateRequestDto
            {
                ProductionEntryNumber = "ENT-001",
                WorkOrderNumber = "WO-001",
                MachineCode = "MCH-001",
                MachineName = "Machine 1",
                Operator = "Operator A",
                Shift = Shift.A,
                Stage = "Assembly",
                ProductCode = "PRD-001",
                ProductName = "Finished Product A",
                BatchNumber = "BATCH-001",
                Parameter = "Length",
                Tolerance = "+/- 0.5mm",
                ExpectedValue = "10.0mm",
                ActualValue = "9.9mm",
                Remarks = "Passed basic checks",
                Notes = "No notes"
            };

            Assert.Equal("ENT-001", req.ProductionEntryNumber);
            Assert.Equal("Operator A", req.Operator);
            Assert.Equal(Shift.A, req.Shift);
            Assert.Equal("10.0mm", req.ExpectedValue);
        }

        [Fact]
        public void FinalCreateRequestDto_exposes_expected_contract_properties()
        {
            var req = new FinalCreateRequestDto
            {
                InspectionDate = System.DateTime.UtcNow,
                FinishedProductCode = "PRD-001",
                FinishedProductName = "Finished Product A",
                ProductionEntryNumber = "ENT-001",
                ProductionBatch = "BATCH-001",
                Inspector = "Inspector A",
                Dimension = "10x20",
                Weight = "500g",
                Strength = "High",
                SurfaceFinish = "Smooth",
                VisualCheck = "Pass",
                AcceptedQuantity = 100m,
                RejectedQuantity = 2m,
                Remarks = "All parameters checked",
                Notes = "N/A",
                Parameters = new System.Collections.Generic.List<FinalInspectionParameterDto>()
            };

            Assert.Equal("Inspector A", req.Inspector);
            Assert.Equal(100m, req.AcceptedQuantity);
            Assert.Equal("500g", req.Weight);
            Assert.Empty(req.Parameters);
        }

        [Fact]
        public void QualityControlRules_validates_final_inspection_quantities()
        {
            var err1 = QualityControlRules.ValidateFinalInspectionQuantities(50m, 10m, 100m);
            Assert.Null(err1);

            var errNeg = QualityControlRules.ValidateFinalInspectionQuantities(-5m, 10m, 100m);
            Assert.NotNull(errNeg);
            Assert.Contains("cannot be negative", errNeg);

            var errExcess = QualityControlRules.ValidateFinalInspectionQuantities(80m, 30m, 100m);
            Assert.NotNull(errExcess);
            Assert.Contains("cannot exceed production entry good quantity", errExcess);
        }

        [Fact]
        public void QualityControlRules_validates_rejection_quantities()
        {
            var err1 = QualityControlRules.ValidateRejectionQuantities(5m, 10m);
            Assert.Null(err1);

            var errZero = QualityControlRules.ValidateRejectionQuantities(0m, 10m);
            Assert.NotNull(errZero);
            Assert.Contains("greater than zero", errZero);

            var errExcess = QualityControlRules.ValidateRejectionQuantities(12m, 10m);
            Assert.NotNull(errExcess);
            Assert.Contains("cannot exceed applicable production quantity", errExcess);
        }

        [Fact]
        public async Task QualityControl_GetPermissions_returns_expected_RBAC_list()
        {
            var service = new ERP.Infrastructure.Procurement.QualityControlService(null!, null!);
            var perms = await service.GetPermissionsAsync();

            Assert.Contains("quality-control.view", perms);
            Assert.Contains("quality-control.create", perms);
            Assert.Contains("quality-control.approve", perms);
            Assert.Contains("quality-control.inspect", perms);
            Assert.Contains("quality-control.certificate", perms);
        }
    }
}
