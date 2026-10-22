namespace ERP.Domain.Sales
{
    public enum VolumeSlabResolutionStrategy
    {
        HighestQualifyingFloor = 0,
        StrictIntervalOrBaseRate = 1
    }

    public enum PriceListTierType
    {
        Global = 0,
        Distributor = 1,
        Wholesale = 2,
        Retail = 3,
        OEM = 4,
        Contract = 5
    }

    public enum SalesChannelType
    {
        Direct = 0,
        B2BPortal = 1,
        DealerNetwork = 2,
        Export = 3,
        ECommerce = 4
    }

    public enum PricingItemType
    {
        FinishedGood = 0,
        RawMaterial = 1,
        Service = 2
    }
}
