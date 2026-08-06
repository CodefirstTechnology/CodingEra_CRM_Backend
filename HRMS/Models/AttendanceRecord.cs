using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("attendance_records")]
public class AttendanceRecord
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("attendance_date")]
    public DateOnly AttendanceDate { get; set; }

    [Column("check_in")]
    public TimeOnly? CheckIn { get; set; }

    [Column("check_out")]
    public TimeOnly? CheckOut { get; set; }

    [Column("working_minutes")]
    public int? WorkingMinutes { get; set; }

    [Column("break_duration_minutes")]
    public int? BreakDurationMinutes { get; set; }

    [Column("overtime_minutes")]
    public int? OvertimeMinutes { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = AttendanceStatus.Present;

    [Column("is_late")]
    public bool IsLate { get; set; }

    [Column("is_early_leave")]
    public bool IsEarlyLeave { get; set; }

    [Column("clock_in_device_at")]
    public DateTime? ClockInDeviceAt { get; set; }

    [Column("clock_in_server_at")]
    public DateTime? ClockInServerAt { get; set; }

    [Column("clock_out_device_at")]
    public DateTime? ClockOutDeviceAt { get; set; }

    [Column("clock_out_server_at")]
    public DateTime? ClockOutServerAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
