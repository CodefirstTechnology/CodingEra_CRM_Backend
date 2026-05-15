using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("contacts")]
    public class Contact
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("salutation")]
        [MaxLength(32)]
        public string Salutation { get; set; } = string.Empty;

        [Column("first_name")]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(64)]
        public string Phone { get; set; } = string.Empty;

        [Column("gender")]
        [MaxLength(32)]
        public string Gender { get; set; } = string.Empty;

        [Column("organization_id")]
        public int? OrganizationId { get; set; }

        [Column("designation")]
        [MaxLength(256)]
        public string Designation { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("last_modified")]
        public DateTime LastModified { get; set; }
    }
}
