namespace ERP.Domain.Sales
{
    public class QuotationApprovalComment
    {
        public int Id { get; set; }

        public int QuotationApprovalId { get; set; }

        public QuotationApproval QuotationApproval { get; set; } = null!;

        public string Comment { get; set; } = string.Empty;

        public string CommentedBy { get; set; } = string.Empty;

        public DateTimeOffset CommentedOn { get; set; }
    }
}
