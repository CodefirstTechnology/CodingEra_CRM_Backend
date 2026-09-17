using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models;

[Table("company_profiles")]
public class CompanyProfile : ITenantEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Column("company_name")]
    [MaxLength(256)]
    public string CompanyName { get; set; } = string.Empty;

    [Column("legal_name")]
    [MaxLength(256)]
    public string? LegalName { get; set; }

    [Column("company_code")]
    [MaxLength(64)]
    public string? CompanyCode { get; set; }

    [Column("registration_number")]
    [MaxLength(128)]
    public string? RegistrationNumber { get; set; }

    [Column("industry")]
    [MaxLength(128)]
    public string? Industry { get; set; }

    [Column("company_type")]
    [MaxLength(64)]
    public string? CompanyType { get; set; }

    [Column("website")]
    [MaxLength(256)]
    public string? Website { get; set; }

    [Column("email")]
    [MaxLength(256)]
    public string? Email { get; set; }

    [Column("phone")]
    [MaxLength(64)]
    public string? Phone { get; set; }

    [Column("address_line1")]
    [MaxLength(256)]
    public string? AddressLine1 { get; set; }

    [Column("address_line2")]
    [MaxLength(256)]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    [MaxLength(128)]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(128)]
    public string? State { get; set; }

    [Column("country")]
    [MaxLength(128)]
    public string? Country { get; set; }

    [Column("pin_code")]
    [MaxLength(32)]
    public string? PinCode { get; set; }

    [Column("primary_contact_person")]
    [MaxLength(128)]
    public string? PrimaryContactPerson { get; set; }

    [Column("contact_email")]
    [MaxLength(256)]
    public string? ContactEmail { get; set; }

    [Column("contact_phone")]
    [MaxLength(64)]
    public string? ContactPhone { get; set; }

    [Column("pan")]
    [MaxLength(32)]
    public string? Pan { get; set; }

    [Column("tan")]
    [MaxLength(32)]
    public string? Tan { get; set; }

    [Column("gstin")]
    [MaxLength(32)]
    public string? Gstin { get; set; }

    [Column("pf_registration_number")]
    [MaxLength(64)]
    public string? PfRegistrationNumber { get; set; }

    [Column("esic_registration_number")]
    [MaxLength(64)]
    public string? EsicRegistrationNumber { get; set; }

    [Column("pt_registration_details")]
    [MaxLength(128)]
    public string? PtRegistrationDetails { get; set; }

    [Column("logo_url")]
    [MaxLength(512)]
    public string? LogoUrl { get; set; }

    [Column("default_currency")]
    [MaxLength(16)]
    public string DefaultCurrency { get; set; } = "INR";

    [Column("timezone")]
    [MaxLength(64)]
    public string Timezone { get; set; } = "Asia/Kolkata";

    [Column("date_format")]
    [MaxLength(32)]
    public string DateFormat { get; set; } = "DD/MM/YYYY";

    [Column("financial_year")]
    [MaxLength(32)]
    public string FinancialYear { get; set; } = "Apr - Mar";

    [Column("payroll_cycle")]
    [MaxLength(32)]
    public string PayrollCycle { get; set; } = "Monthly (1st - 30th/31st)";

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
