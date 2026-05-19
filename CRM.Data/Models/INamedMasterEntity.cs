namespace CRM.models;

/// <summary>Shared shape for lookup master rows (industries, territories, roles, etc.).</summary>
public interface INamedMasterEntity : IMasterDataEntity
{
    int Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }
    bool IsActive { get; set; }
}
