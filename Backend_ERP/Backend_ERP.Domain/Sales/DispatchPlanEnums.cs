using System.Text.Json.Serialization;

namespace ERP.Domain.Sales
{
    public enum DispatchPlanStatus
    {
        Draft,
        Planned,
        Approved,
        ReadyForDispatch,
        Closed
    }

    public enum DispatchPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}
