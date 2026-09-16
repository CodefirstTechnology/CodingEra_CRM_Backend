using System;

namespace ERP.Domain.Accounting
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string AccountType { get; set; } = "Current";
        public string Currency { get; set; } = "INR";
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    }
}
