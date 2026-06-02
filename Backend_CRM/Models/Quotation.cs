using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    public static class QuotationStatuses
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";

        public static readonly string[] All = { Draft, Sent, Approved, Rejected, Expired };
    }

    [Table("quotations")]
    public class Quotation : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("deal_id")]
        public int? DealId { get; set; }

        [Column("salutation")]
        [MaxLength(32)]
        public string Salutation { get; set; } = string.Empty;

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("gender")]
        [MaxLength(32)]
        public string Gender { get; set; } = string.Empty;

        [Column("customer_name")]
        [MaxLength(256)]
        public string CustomerName { get; set; } = string.Empty;

        [Column("company_name")]
        [MaxLength(512)]
        public string CompanyName { get; set; } = string.Empty;

        [Column("employees")]
        [MaxLength(128)]
        public string Employees { get; set; } = string.Empty;

        [Column("annual_revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Column("website")]
        [MaxLength(512)]
        public string Website { get; set; } = string.Empty;

        [Column("gst")]
        [MaxLength(32)]
        public string Gst { get; set; } = string.Empty;

        [Column("territory")]
        [MaxLength(256)]
        public string Territory { get; set; } = string.Empty;

        [Column("industry")]
        [MaxLength(256)]
        public string Industry { get; set; } = string.Empty;

        [Column("contact_person")]
        [MaxLength(256)]
        public string ContactPerson { get; set; } = string.Empty;

        [Column("mobile_number")]
        [MaxLength(64)]
        public string MobileNumber { get; set; } = string.Empty;

        [Column("email_address")]
        [MaxLength(256)]
        public string EmailAddress { get; set; } = string.Empty;

        [Column("office_address")]
        public string OfficeAddress { get; set; } = string.Empty;

        [Column("site_address")]
        public string SiteAddress { get; set; } = string.Empty;

        [Column("reference_number")]
        [MaxLength(128)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Column("reference_date")]
        public DateTime? ReferenceDate { get; set; }

        [Column("company_code")]
        [MaxLength(32)]
        public string CompanyCode { get; set; } = string.Empty;

        [Column("document_type_code")]
        [MaxLength(32)]
        public string DocumentTypeCode { get; set; } = "QTN";

        [Column("fiscal_year_label")]
        [MaxLength(16)]
        public string FiscalYearLabel { get; set; } = string.Empty;

        [Column("sequence_number")]
        public int SequenceNumber { get; set; }

        [Column("quotation_number")]
        [MaxLength(64)]
        public string QuotationNumber { get; set; } = string.Empty;

        [Column("quotation_date")]
        public DateTime QuotationDate { get; set; }

        [Column("status")]
        [MaxLength(32)]
        public string Status { get; set; } = QuotationStatuses.Draft;

        [Column("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [Column("grand_total")]
        public decimal GrandTotal { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("tax_total")]
        public decimal TaxTotal { get; set; }

        [Column("total_quantity")]
        public decimal TotalQuantity { get; set; }

        [Column("total_weight")]
        public decimal TotalWeight { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [JsonIgnore]
        public ICollection<QuotationLineItem> LineItems { get; set; } = new List<QuotationLineItem>();
    }
}
