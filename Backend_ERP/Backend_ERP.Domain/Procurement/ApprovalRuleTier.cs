namespace ERP.Domain.Procurement
{
    public class ApprovalRuleTier
    {
        public int Id { get; set; }

        public int ApprovalRuleId { get; set; }

        public ApprovalRule? ApprovalRule { get; set; }

        public int SequenceNumber { get; set; }

        public string RequiredRole { get; set; } = string.Empty;

        public decimal MinAmount { get; set; }

        public decimal MaxAmount { get; set; }
    }
}
