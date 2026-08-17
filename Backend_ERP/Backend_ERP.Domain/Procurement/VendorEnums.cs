namespace ERP.Domain.Procurement
{
    public enum VendorStatus
    {
        Draft,
        PendingApproval,
        Approved,
        Rejected,
        Active,
        Inactive,
        Blocked
    }

    public enum VendorAddressType
    {
        Billing,
        Shipping,
        Office,
        Warehouse,
        Factory
    }

    public enum VendorComplianceStatus
    {
        Compliant,
        NonCompliant,
        PendingVerification,
        Expired
    }
}
