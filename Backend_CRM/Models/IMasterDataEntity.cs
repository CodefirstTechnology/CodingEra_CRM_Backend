namespace CRM.models
{
    /// <summary>Lookup master rows whose timestamps are set in <c>TaskDbcontext</c> on save.</summary>
    public interface IMasterDataEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        DateTime LastModified { get; set; }
    }
}
