using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("organizations")]
    public class Organization
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(512)]
        public string Name { get; set; } = string.Empty;

        [Column("website")]
        [MaxLength(512)]
        public string Website { get; set; } = string.Empty;

        [Column("industry")]
        [MaxLength(256)]
        public string Industry { get; set; } = string.Empty;

        [Column("annual_revenue")]
        public decimal? AnnualRevenue { get; set; }

        [Column("employees")]
        [MaxLength(128)]
        public string Employees { get; set; } = string.Empty;

        [Column("territory")]
        [MaxLength(256)]
        public string Territory { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("last_modified")]
        public DateTime LastModified { get; set; }
    }
}
