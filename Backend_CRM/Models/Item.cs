using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    public enum ItemStatus
    {
        Active = 0,
        Inactive = 1,
    }

    [Table("items")]
    public class Item : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("item_code")]
        [MaxLength(64)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("item_name")]
        [MaxLength(512)]
        public string ItemName { get; set; } = string.Empty;

        [Column("item_group_id")]
        public int? ItemGroupId { get; set; }

        [ForeignKey(nameof(ItemGroupId))]
        public ItemGroup? ItemGroup { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("status")]
        public ItemStatus Status { get; set; } = ItemStatus.Active;

        [Column("has_variants")]
        public bool HasVariants { get; set; }

        [Column("parent_item_id")]
        public int? ParentItemId { get; set; }

        [ForeignKey(nameof(ParentItemId))]
        public Item? ParentItem { get; set; }

        public ICollection<Item> Variants { get; set; } = new List<Item>();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        public ICollection<ItemSpecification> Specifications { get; set; } = new List<ItemSpecification>();

        public ICollection<ItemVariantAttributeValue> VariantAttributeValues { get; set; } =
            new List<ItemVariantAttributeValue>();

        public ICollection<ItemTemplateAttribute> TemplateAttributes { get; set; } =
            new List<ItemTemplateAttribute>();
    }
}
