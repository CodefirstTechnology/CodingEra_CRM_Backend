using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Admin-defined default column layout for the quotation item grid (single row, id = 1).</summary>
    [Table("quotation_item_grid_defaults")]
    public class QuotationItemGridDefault
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } = 1;

        [Column("columns_json")]
        public string ColumnsJson { get; set; } = string.Empty;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
