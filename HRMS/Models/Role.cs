using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("roles")]
public class Role
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(256)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
