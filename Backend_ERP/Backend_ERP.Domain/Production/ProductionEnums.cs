namespace ERP.Domain.Production
{
    public enum BomStatus
    {
        Draft,
        Approved,
        Active,
        Archived
    }

    public enum PlanStatus
    {
        Draft,
        Approved,
        Released,
        Completed
    }

    public enum WorkOrderStatus
    {
        Draft,
        Released,
        InProgress,
        Paused,
        Completed,
        Closed
    }

    public enum ScheduleStatus
    {
        Scheduled,
        Running,
        Completed,
        Delayed,
        Cancelled
    }

    public enum MachineStatus
    {
        Running,
        Idle,
        Breakdown,
        Maintenance
    }

    public enum EntryStatus
    {
        Draft,
        Submitted,
        Approved,
        Posted
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
