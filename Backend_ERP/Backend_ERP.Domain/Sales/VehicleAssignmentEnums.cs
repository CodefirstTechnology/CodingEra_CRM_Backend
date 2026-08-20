namespace ERP.Domain.Sales
{
    public enum VehicleAssignmentStatus
    {
        Draft,
        Assigned,
        Loaded,
        Dispatched,
        Completed,
        Cancelled
    }

    public enum VehicleType
    {
        Truck,
        Trailer,
        Container,
        Tempo,
        Pickup
    }

    public enum TransportMode
    {
        Road,
        Rail,
        Air,
        Sea,
        Multimodal
    }

    public enum TransportStatus
    {
        Draft,
        InTransit,
        Delivered,
        Closed
    }
}
