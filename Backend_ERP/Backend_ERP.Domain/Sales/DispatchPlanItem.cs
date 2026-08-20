using System;

namespace ERP.Domain.Sales
{
    public class DispatchPlanItem
    {
        public int Id { get; set; }

        public int DispatchPlanId { get; set; }

        public DispatchPlan DispatchPlan { get; set; } = null!;

        public int FinishedGoodId { get; set; }

        public string FinishedGoodCode { get; set; } = string.Empty;

        public string FinishedGoodName { get; set; } = string.Empty;

        public string BatchNumber { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "NOS";

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public int? FinalInspectionId { get; set; }

        public string? FinalInspectionNumber { get; set; }

        public int? TestCertificateId { get; set; }

        public string? TestCertificateNumber { get; set; }
    }
}
