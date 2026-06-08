using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("quotation_line_items")]
    public class QuotationLineItem
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("quotation_id")]
        public int QuotationId { get; set; }

        [ForeignKey(nameof(QuotationId))]
        [JsonIgnore]
        public Quotation? Quotation { get; set; }

        [Column("line_index")]
        public int LineIndex { get; set; }

        [Column("item_id")]
        public int? ItemId { get; set; }

        [Column("item_code")]
        [MaxLength(64)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_name")]
        [MaxLength(256)]
        public string ItemName { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("quantity")]
        public decimal Quantity { get; set; } = 1;

        [Column("uom")]
        [MaxLength(32)]
        public string Uom { get; set; } = string.Empty;

        [Column("weight")]
        public decimal Weight { get; set; }

        [Column("unit_weight")]
        public decimal UnitWeight { get; set; }

        [Column("rate")]
        public decimal Rate { get; set; }

        [Column("steel_rate")]
        public decimal SteelRate { get; set; }

        /// <summary>JSON snapshot of item attributes/specs at quote time.</summary>
        [Column("item_snapshot_json")]
        public string ItemSnapshotJson { get; set; } = string.Empty;

        [Column("discount_percent")]
        public decimal DiscountPercent { get; set; }

        [Column("gst_percent")]
        public decimal GstPercent { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("tax_amount")]
        public decimal TaxAmount { get; set; }

        [Column("line_total")]
        public decimal LineTotal { get; set; }
    }
}
