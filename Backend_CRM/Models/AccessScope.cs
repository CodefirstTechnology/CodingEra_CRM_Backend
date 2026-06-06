namespace CRM.models
{
    /// <summary>Record visibility scope for a role permission (ERPNext/Frappe style).</summary>
    public enum AccessScope
    {
        Own = 0,
        Team = 1,
        All = 2,
    }
}
