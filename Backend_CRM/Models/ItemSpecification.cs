using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("item_specifications")]
    public class ItemSpecification
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Column("spec_name")]
        [MaxLength(128)]
        public string SpecName { get; set; } = string.Empty;

        [Column("spec_value")]
        [MaxLength(512)]
        public string SpecValue { get; set; } = string.Empty;

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
