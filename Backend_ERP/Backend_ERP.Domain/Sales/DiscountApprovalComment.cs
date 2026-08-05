namespace ERP.Domain.Sales
{
    public class DiscountApprovalComment
    {
        public int Id { get; set; }

        public int DiscountApprovalId { get; set; }

        public DiscountApproval DiscountApproval { get; set; } = null!;

        public string Comment { get; set; } = string.Empty;

        public string CommentedBy { get; set; } = string.Empty;

        public DateTimeOffset CommentedOn { get; set; }
    }
}
