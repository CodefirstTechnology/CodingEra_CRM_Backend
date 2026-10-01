namespace ERP.Domain.Procurement
{
    public class FinalInspectionParameter
    {
        public int Id { get; set; }

        public int FinalInspectionId { get; set; }

        public FinalInspection? FinalInspection { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Expected { get; set; } = string.Empty;

        public string Actual { get; set; } = string.Empty;

        public string UoM { get; set; } = "mm";

        public decimal? TargetValue { get; set; }

        public decimal? MinTolerance { get; set; }

        public decimal? MaxTolerance { get; set; }

        public decimal? ActualNumericValue { get; set; }

        public QcCheckResult Result { get; set; } = QcCheckResult.Pending;
    }
}
