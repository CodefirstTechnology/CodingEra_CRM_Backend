using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("quotation_settings")]
    public class QuotationSettings
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } = 1;

        [Column("company_code")]
        [MaxLength(32)]
        public string CompanyCode { get; set; } = string.Empty;

        [Column("document_type_code")]
        [MaxLength(32)]
        public string DocumentTypeCode { get; set; } = "QTN";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
