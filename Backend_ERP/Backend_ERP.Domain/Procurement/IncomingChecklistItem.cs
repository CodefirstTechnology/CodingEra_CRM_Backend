namespace ERP.Domain.Procurement
{
    public class IncomingChecklistItem
    {
        public int Id { get; set; }

        public int IncomingInspectionId { get; set; }

        public IncomingInspection? IncomingInspection { get; set; }

        public string Parameter { get; set; } = string.Empty;

        public string Specification { get; set; } = string.Empty;

        public string ActualValue { get; set; } = string.Empty;

        public QcCheckResult Result { get; set; } = QcCheckResult.Pending;

        public string Remarks { get; set; } = string.Empty;
    }
}
