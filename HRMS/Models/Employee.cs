using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("employees")]
public class Employee
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("employee_code")]
    [MaxLength(32)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("full_name")]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Column("phone_number")]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("department_id")]
    public int DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [Column("designation_id")]
    public int DesignationId { get; set; }

    [ForeignKey(nameof(DesignationId))]
    public Designation? Designation { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Column("joining_date")]
    public DateOnly JoiningDate { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("gender")]
    [MaxLength(32)]
    public string? Gender { get; set; }

    [Column("blood_group")]
    [MaxLength(8)]
    public string? BloodGroup { get; set; }

    [Column("bank_name")]
    [MaxLength(128)]
    public string? BankName { get; set; }

    [Column("account_number")]
    [MaxLength(32)]
    public string? AccountNumber { get; set; }

    [Column("ifsc_code")]
    [MaxLength(16)]
    public string? IfscCode { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
