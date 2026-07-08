using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("item_attribute_values")]
    public class ItemAttributeValue
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("attribute_id")]
        public int AttributeId { get; set; }

        [ForeignKey(nameof(AttributeId))]
        public ItemAttribute Attribute { get; set; } = null!;

        [Column("value")]
        [MaxLength(256)]
        public string Value { get; set; } = string.Empty;

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
