using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("leave_allocations")]
public class LeaveAllocation
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

    [Column("year")]
    public int Year { get; set; }

    [Column("allocated_days")]
    public int AllocatedDays { get; set; }

    [Column("used_days")]
    public int UsedDays { get; set; }
}
