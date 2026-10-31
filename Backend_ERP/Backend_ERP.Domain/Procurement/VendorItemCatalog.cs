using System;

namespace ERP.Domain.Procurement
{
    public class VendorItemCatalog
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string ItemType { get; set; } = "RawMaterial"; // RawMaterial, Component, Consumable

        public int ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string? SupplierPartNumber { get; set; }

        public int StandardLeadTimeDays { get; set; } = 14;

        public decimal MinimumOrderQuantity { get; set; } = 1.0000m;

        public decimal OrderMultiple { get; set; } = 1.0000m;

        public string PackagingUnit { get; set; } = "Units";

        public decimal BaseUomConversionMultiplier { get; set; } = 1.000000m;

        public decimal UnitPriceSnapshot { get; set; }

        public string Currency { get; set; } = "INR";

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool IsApproved { get; set; } = true;

        public string? TechnicalRemarks { get; set; }
    }
}

