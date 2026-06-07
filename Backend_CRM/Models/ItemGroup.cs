using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("item_groups")]
    public class ItemGroup : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public ItemGroup? Parent { get; set; }

        public ICollection<ItemGroup> Children { get; set; } = new List<ItemGroup>();

        [Column("description")]
        public string Description { get; set; } = string.Empty;

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
    }
}
