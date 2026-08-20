namespace ERP.Domain.Sales
{
    public class VehicleAssignmentDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "VA";

        public int LastSequence { get; set; }
    }

    public class TransportDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "TR";

        public int LastSequence { get; set; }
    }
}
