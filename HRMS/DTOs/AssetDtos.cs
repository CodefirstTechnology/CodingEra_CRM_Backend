using System.ComponentModel.DataAnnotations;

namespace HRMS.DTOs;

public class AssetUpsertDto
{
    public int? BranchId { get; set; }

    [Required]
    [MaxLength(64)]
    public string AssetCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Category { get; set; } = "Laptop";

    [MaxLength(128)]
    public string? SerialNumber { get; set; }

    [MaxLength(128)]
    public string? ModelNumber { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Available"; // Available, Assigned, Maintenance, Retired

    public DateOnly? PurchaseDate { get; set; }

    public int? AssignedToEmployeeId { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}

public class AssetResponseDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? PurchaseDate { get; set; }
    public int? AssignedToEmployeeId { get; set; }
    public string? AssignedToEmployeeName { get; set; }
    public string? AssignedToEmployeeCode { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AssetAssignDto
{
    public int? EmployeeId { get; set; }
    public string? Notes { get; set; }
}
