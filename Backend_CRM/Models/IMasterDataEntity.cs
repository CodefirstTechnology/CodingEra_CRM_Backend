namespace CRM.models
{
    /// <summary>Lookup master rows whose <see cref="LastModified"/> is set in <c>TaskDbcontext</c> on save.</summary>
    public interface IMasterDataEntity
    {
        DateTime LastModified { get; set; }
    }
}
