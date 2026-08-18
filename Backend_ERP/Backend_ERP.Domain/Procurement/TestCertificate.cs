using System;

namespace ERP.Domain.Procurement
{
    public class TestCertificate
    {
        public int Id { get; set; }

        public string CertificateNumber { get; set; } = string.Empty;

        public DateTime CertificateDate { get; set; } = DateTime.UtcNow;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int FinalInspectionId { get; set; }

        public string FinalInspectionNumber { get; set; } = string.Empty;

        public int? LoadTestId { get; set; }

        public string? LoadTestNumber { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public string IssuedBy { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }

        public CertificateStatus Status { get; set; } = CertificateStatus.Draft;

        public DateTime? ExpiryDate { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
