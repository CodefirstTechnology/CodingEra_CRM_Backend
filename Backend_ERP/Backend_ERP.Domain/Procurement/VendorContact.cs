namespace ERP.Domain.Procurement
{
    public class VendorContact
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Designation { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
