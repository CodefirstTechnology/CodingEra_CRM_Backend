namespace ERP.Domain.Procurement
{
    public enum VendorStatus
    {
        Draft = 1,
        PendingKycApproval = 2,
        PendingApproval = 2,
        Active = 3,
        Approved = 3,
        OnHold = 4,
        Blocked = 4,
        Disqualified = 5,
        Rejected = 5,
        Inactive = 6
    }

    public enum VendorType
    {
        Manufacturer = 1,
        Trader = 2,
        Importer = 3,
        ServiceProvider = 4,
        AuthorizedDistributor = 5
    }

    public enum MsmeCategory
    {
        None = 0,
        Micro = 1,
        Small = 2,
        Medium = 3
    }

    public enum BankAccountVerificationStatus
    {
        Pending = 1,
        PennyDropVerified = 2,
        ManualVerified = 3,
        Failed = 4
    }

    public enum ItemCatalogType
    {
        RawMaterial = 1,
        Component = 2,
        Consumable = 3,
        CapitalEquipment = 4,
        SubContractService = 5
    }

    public enum VendorAddressType
    {
        Billing = 1,
        Shipping = 2,
        Office = 3,
        Warehouse = 4,
        Factory = 5
    }

    public enum VendorComplianceStatus
    {
        Compliant = 1,
        NonCompliant = 2,
        PendingVerification = 3,
        Expired = 4
    }
}
