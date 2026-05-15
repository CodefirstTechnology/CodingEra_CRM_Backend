using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("organizations")]
    public class Organization
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(512)]
        public string Name { get; set; } = string.Empty;

        [Column("website")]
        [MaxLength(512)]
        public string Website { get; set; } = string.Empty;

        [Column("industry_id")]
        public int? IndustryId { get; set; }

        [ForeignKey(nameof(IndustryId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Industry? Industry { get; set; }

        [Column("annual_revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Column("employee_count_id")]
        public int? EmployeeCountId { get; set; }

        [ForeignKey(nameof(EmployeeCountId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public EmployeeCount? EmployeeCount { get; set; }

        [Column("territory_id")]
        public int? TerritoryId { get; set; }

        [ForeignKey(nameof(TerritoryId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Territory? Territory { get; set; }

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("last_modified")]
        public DateTime LastModified { get; set; }
    }
}
