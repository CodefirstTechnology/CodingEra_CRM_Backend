using System.Collections.Generic;

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

        public string UoM { get; set; } = "mm";

        public decimal? TargetValue { get; set; }

        public decimal? MinTolerance { get; set; }

        public decimal? MaxTolerance { get; set; }

        public decimal? MeanValue { get; set; }

        public decimal? StdDeviation { get; set; }

        public QcCheckResult Result { get; set; } = QcCheckResult.Pending;

        public string Remarks { get; set; } = string.Empty;

        public List<IncomingInspectionSample> Samples { get; set; } = new();
    }
}
