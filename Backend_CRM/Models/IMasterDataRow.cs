namespace CRM.models
{
    /// <summary>Shared shape for CRM lookup master tables managed via <c>/api/master-data</c>.</summary>
    public interface IMasterDataRow : IMasterDataEntity, IAuditableByUser
    {
        int Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        bool IsActive { get; set; }
    }
}
