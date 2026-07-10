using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Singleton company profile for quotation PDFs and branding (id = 1).</summary>
    [Table("company_profiles")]
    public class CompanyProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } = 1;

        [Column("brand_name")]
        [MaxLength(128)]
        public string BrandName { get; set; } = string.Empty;

        [Column("company_name")]
        [MaxLength(512)]
        public string CompanyName { get; set; } = string.Empty;

        [Column("tagline")]
        [MaxLength(512)]
        public string Tagline { get; set; } = string.Empty;

        [Column("business_line")]
        public string BusinessLine { get; set; } = string.Empty;

        [Column("logo_content_type")]
        [MaxLength(64)]
        public string LogoContentType { get; set; } = string.Empty;

        [Column("logo_base64")]
        public string LogoBase64 { get; set; } = string.Empty;

        [Column("favicon_content_type")]
        [MaxLength(64)]
        public string FaviconContentType { get; set; } = string.Empty;

        [Column("favicon_base64")]
        public string FaviconBase64 { get; set; } = string.Empty;

        [Column("gstin")]
        [MaxLength(32)]
        public string Gstin { get; set; } = string.Empty;

        [Column("cin_number")]
        [MaxLength(64)]
        public string CinNumber { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("contact_number")]
        [MaxLength(64)]
        public string ContactNumber { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(512)]
        public string Email { get; set; } = string.Empty;

        [Column("website")]
        [MaxLength(256)]
        public string Website { get; set; } = string.Empty;

        [Column("bank_name")]
        [MaxLength(256)]
        public string BankName { get; set; } = string.Empty;

        [Column("account_number")]
        [MaxLength(64)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column("ifsc_code")]
        [MaxLength(32)]
        public string IfscCode { get; set; } = string.Empty;

        [Column("branch_name")]
        [MaxLength(256)]
        public string BranchName { get; set; } = string.Empty;

        [Column("signatory_name")]
        [MaxLength(256)]
        public string SignatoryName { get; set; } = string.Empty;

        [Column("signatory_mobile")]
        [MaxLength(64)]
        public string SignatoryMobile { get; set; } = string.Empty;

        /// <summary>JSON array of { title, body } term rows for quotation PDF.</summary>
        [Column("terms_conditions_json")]
        public string TermsConditionsJson { get; set; } = string.Empty;

        [Column("intro_text")]
        public string IntroText { get; set; } = string.Empty;

        [Column("transportation_label")]
        [MaxLength(128)]
        public string TransportationLabel { get; set; } = string.Empty;

        [Column("jurisdiction")]
        [MaxLength(256)]
        public string Jurisdiction { get; set; } = string.Empty;

        [Column("default_gst_percent")]
        public decimal DefaultGstPercent { get; set; } = 18m;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
