using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Domain.Production
{
    public class BillOfMaterials
    {
        public int Id { get; set; }

        public string BomNumber { get; set; } = string.Empty;

        public string Version { get; set; } = "1.0";

        public int ProductId { get; set; }

        public FinishedGood? Product { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Revision { get; set; } = string.Empty;

        public BomStatus Status { get; set; } = BomStatus.Draft;

        public DateOnly EffectiveDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public List<BomMaterialLine> Materials { get; set; } = new();

        public string ProcessNotes { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string? AttachmentName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
