using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Junction table mapping which features are enabled for each user role.
    /// Roles: Owner, Staff, SuperAdmin
    /// </summary>
    public class RoleFeature
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Role name: "Owner", "Staff", "SuperAdmin"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [Required]
        public int FeatureId { get; set; }

        public bool IsEnabled { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(FeatureId))]
        public AppFeature Feature { get; set; } = null!;
    }
}
