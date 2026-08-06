namespace ERP.Domain.Sales
{
    public class QuotationApproval
    {
        public int Id { get; set; }

        public string ApprovalNumber { get; set; } = string.Empty;

        public DateOnly RequestDate { get; set; }

        public int QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int? SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int SalesPersonUserId { get; set; }

        public decimal TotalAmount { get; set; }

        public string ApprovalLevel { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<QuotationApprovalHistory> History { get; set; } = new List<QuotationApprovalHistory>();

        public ICollection<QuotationApprovalComment> Comments { get; set; } = new List<QuotationApprovalComment>();
    }
}
