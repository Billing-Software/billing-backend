using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Junction table mapping which features are enabled for each subscription plan.
    /// </summary>
    public class PlanFeature
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlanId { get; set; }

        [Required]
        public int FeatureId { get; set; }

        public bool IsEnabled { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(PlanId))]
        public SubscriptionPlan Plan { get; set; } = null!;

        [ForeignKey(nameof(FeatureId))]
        public AppFeature Feature { get; set; } = null!;
    }
}
