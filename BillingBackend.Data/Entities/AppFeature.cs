using System.ComponentModel.DataAnnotations;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Master registry of all application features. Each feature has a unique key
    /// used to control access based on subscription plans and user roles.
    /// </summary>
    public class AppFeature
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique identifier key used in code, e.g. "billing", "inventory", "branches"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FeatureKey { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name, e.g. "Billing & Invoicing"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Short description of what the feature does
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Grouping category: Core, Standard, Advanced, Premium
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Core";

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
        public ICollection<RoleFeature> RoleFeatures { get; set; } = new List<RoleFeature>();
    }
}
