namespace ERP.Domain.Procurement
{
    public class VendorPaymentTerm
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Days { get; set; }

        public decimal AdvancePercentage { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
