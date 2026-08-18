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

        public QcCheckResult Result { get; set; } = QcCheckResult.Pending;
    }
}
