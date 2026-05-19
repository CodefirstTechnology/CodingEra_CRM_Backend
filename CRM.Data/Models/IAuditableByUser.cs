namespace CRM.models
{
    /// <summary>Optional FKs to <c>users.id</c> for row-level audit (API supplies acting user via <see cref="CRM.DATA.TaskDbcontext.AuditUserId"/>).</summary>
    public interface IAuditableByUser
    {
        int? CreatedBy { get; set; }
        int? UpdatedBy { get; set; }
    }
}
