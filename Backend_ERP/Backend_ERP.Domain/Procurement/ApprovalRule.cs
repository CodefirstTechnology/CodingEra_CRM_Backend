using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class ApprovalRule
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DocumentType { get; set; } = "PurchaseOrder";

        public bool IsActive { get; set; } = true;

        public List<ApprovalRuleTier> Tiers { get; set; } = new();
    }
}
