using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("leave_requests")]
public class LeaveRequest
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("leave_type_id")]
    public int LeaveTypeId { get; set; }

    [ForeignKey(nameof(LeaveTypeId))]
    public LeaveType? LeaveType { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("total_days")]
    public int TotalDays { get; set; }

    [Column("reason")]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = LeaveStatus.Pending;

    [Column("attachment_path")]
    [MaxLength(512)]
    public string? AttachmentPath { get; set; }

    [Column("attachment_file_name")]
    [MaxLength(256)]
    public string? AttachmentFileName { get; set; }

    [Column("approval_remarks")]
    [MaxLength(512)]
    public string? ApprovalRemarks { get; set; }

    [Column("approved_by_user_id")]
    public int? ApprovedByUserId { get; set; }

    [ForeignKey(nameof(ApprovedByUserId))]
    public User? ApprovedByUser { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
