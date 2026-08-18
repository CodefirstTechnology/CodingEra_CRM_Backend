namespace ERP.Domain.Procurement
{
    public class PhysicalVerificationLine
    {
        public int Id { get; set; }

        public int PhysicalVerificationId { get; set; }

        public PhysicalVerification? PhysicalVerification { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal ExpectedQuantity { get; set; }

        public decimal ActualQuantity { get; set; }

        public decimal Variance { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
