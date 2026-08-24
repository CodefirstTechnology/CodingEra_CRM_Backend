namespace ERP.Domain.Enums
{
    /// <summary>
    /// Represents the access visibility scope for RBAC authorization in the ERP.
    /// </summary>
    public enum AccessScope
    {
        /// <summary>
        /// Own scope: user can only access records owned or created by themselves.
        /// </summary>
        Own = 0,

        /// <summary>
        /// Team scope: user can access records within their assigned team / department.
        /// </summary>
        Team = 1,

        /// <summary>
        /// All scope: user has full organization-wide visibility across all records.
        /// </summary>
        All = 2
    }
}
