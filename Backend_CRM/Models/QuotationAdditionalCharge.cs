using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("quotation_additional_charges")]
    public class QuotationAdditionalCharge
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("quotation_id")]
        public int QuotationId { get; set; }

        [Column("charge_name")]
        [MaxLength(256)]
        public string ChargeName { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("sort_index")]
        public int SortIndex { get; set; }

        [JsonIgnore]
        public Quotation? Quotation { get; set; }
    }
}
