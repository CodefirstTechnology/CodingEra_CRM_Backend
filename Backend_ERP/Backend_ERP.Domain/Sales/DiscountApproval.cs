namespace ERP.Domain.Sales
{
    public class DiscountApproval
    {
        public int Id { get; set; }

        public string ApprovalNumber { get; set; } = string.Empty;

        public DateOnly RequestDate { get; set; }

        public string SourceType { get; set; } = DiscountApprovalSourceTypes.SalesOrder;

        public int? QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int? SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public int? PriceListId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerCategory { get; set; } = PriceListCustomerCategories.Standard;

        public int SalesPersonUserId { get; set; }

        public decimal RequestedDiscountPercentage { get; set; }

        public decimal? ApprovedDiscountPercentage { get; set; }

        public decimal RequestedAmount { get; set; }

        public decimal? ApprovedAmount { get; set; }

        public string ApprovalLevel { get; set; } = DiscountApprovalLevels.Manager;

        public string Priority { get; set; } = DiscountApprovalPriorities.Normal;

        public string Status { get; set; } = DiscountApprovalStatuses.Pending;

        public string Reason { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTimeOffset UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<DiscountApprovalHistory> History { get; set; } =
            new List<DiscountApprovalHistory>();

        public ICollection<DiscountApprovalComment> Comments { get; set; } =
            new List<DiscountApprovalComment>();
    }
}
