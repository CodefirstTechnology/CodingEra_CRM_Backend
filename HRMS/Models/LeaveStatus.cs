namespace HRMS.Models;

public static class LeaveStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending, Approved, Rejected, Cancelled
    };

    public static readonly HashSet<string> Active = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending, Approved
    };
}
