using System;

namespace ERP.Domain.Accounting
{
    public class OutstandingRecord
    {
        public int Id { get; set; }
        public PartyType PartyType { get; set; } = PartyType.Customer;
        public int PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public int DocumentId { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public decimal OriginalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding { get; set; }
        public OutstandingStatus Status { get; set; } = OutstandingStatus.Open;

        public int AgeingDays { get; set; }
        public decimal Bucket0To30 { get; set; }
        public decimal Bucket31To60 { get; set; }
        public decimal Bucket61To90 { get; set; }
        public decimal Bucket90Plus { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
