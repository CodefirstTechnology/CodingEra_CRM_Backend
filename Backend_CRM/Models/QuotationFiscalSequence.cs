using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("quotation_fiscal_sequences")]
    public class QuotationFiscalSequence
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("company_code")]
        [MaxLength(32)]
        public string CompanyCode { get; set; } = string.Empty;

        [Column("fiscal_year_label")]
        [MaxLength(16)]
        public string FiscalYearLabel { get; set; } = string.Empty;

        [Column("last_sequence")]
        public int LastSequence { get; set; }
    }
}
