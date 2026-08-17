namespace ERP.Domain.Procurement
{
    public class VendorAddress
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public VendorAddressType AddressType { get; set; } = VendorAddressType.Billing;

        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = "India";

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
