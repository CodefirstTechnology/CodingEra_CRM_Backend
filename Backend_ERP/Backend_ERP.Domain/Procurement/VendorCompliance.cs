using System;

namespace ERP.Domain.Procurement
{
    public class VendorCompliance
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string GSTIN { get; set; } = string.Empty;

        public string PAN { get; set; } = string.Empty;

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public bool GSTRegistered { get; set; }

        public bool MSMERegistered { get; set; }

        public string MSMENumber { get; set; } = string.Empty;

        public string CertificateNumber { get; set; } = string.Empty;

        public DateTime? CertificateExpiryDate { get; set; }

        public VendorComplianceStatus ComplianceStatus { get; set; } = VendorComplianceStatus.PendingVerification;

        public string Remarks { get; set; } = string.Empty;
    }
}
