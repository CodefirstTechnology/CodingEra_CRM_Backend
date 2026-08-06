using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("leave_types")]
public class LeaveType
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(8)]
    public string Code { get; set; } = string.Empty;

    [Column("default_allocated_days")]
    public int DefaultAllocatedDays { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
