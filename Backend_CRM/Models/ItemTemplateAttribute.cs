using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Defines which attributes are used for variant generation on a parent item.</summary>
    [Table("item_template_attributes")]
    public class ItemTemplateAttribute
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
    }
}
