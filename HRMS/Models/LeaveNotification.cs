using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("leave_notifications")]
public class LeaveNotification
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("leave_request_id")]
    public int? LeaveRequestId { get; set; }

    [ForeignKey(nameof(LeaveRequestId))]
    public LeaveRequest? LeaveRequest { get; set; }

    [Column("title")]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    [MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
