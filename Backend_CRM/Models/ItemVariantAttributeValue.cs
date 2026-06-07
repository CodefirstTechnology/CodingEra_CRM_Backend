using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("item_variant_attribute_values")]
    public class ItemVariantAttributeValue
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Column("attribute_id")]
        public int AttributeId { get; set; }

        [ForeignKey(nameof(AttributeId))]
        public ItemAttribute Attribute { get; set; } = null!;

        [Column("attribute_value_id")]
        public int? AttributeValueId { get; set; }

        [ForeignKey(nameof(AttributeValueId))]
        public ItemAttributeValue? AttributeValue { get; set; }

        [Column("custom_value")]
        [MaxLength(256)]
        public string CustomValue { get; set; } = string.Empty;
    }
}
