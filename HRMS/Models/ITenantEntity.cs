namespace HRMS.Models;

public interface ITenantEntity
{
    int TenantId { get; set; }
}

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    int? CreatedBy { get; set; }
    int? UpdatedBy { get; set; }
}
