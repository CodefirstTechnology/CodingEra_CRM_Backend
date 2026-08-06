using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("payroll_records")]
public class PayrollRecord
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Column("pay_month")]
    public int PayMonth { get; set; }

    [Column("pay_year")]
    public int PayYear { get; set; }

    [Column("basic_salary")]
    public decimal BasicSalary { get; set; }

    [Column("allowances")]
    public decimal Allowances { get; set; }

    [Column("deductions")]
    public decimal Deductions { get; set; }

    [Column("net_salary")]
    public decimal NetSalary { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
