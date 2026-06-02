using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("quotation_item_grid_user_preferences")]
    public class QuotationItemGridUserPreference
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("columns_json")]
        public string ColumnsJson { get; set; } = string.Empty;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
