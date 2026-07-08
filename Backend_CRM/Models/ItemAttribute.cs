using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    public enum ItemAttributeValueType
    {
        Text = 0,
        Number = 1,
        Select = 2,
    }

    [Table("item_attributes")]
    public class ItemAttribute : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Column("code")]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Column("value_type")]
        public ItemAttributeValueType ValueType { get; set; } = ItemAttributeValueType.Text;

        [Column("is_variant_attribute")]
        public bool IsVariantAttribute { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        public ICollection<ItemAttributeValue> Values { get; set; } = new List<ItemAttributeValue>();
    }
}
