  using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IFeatureService
    {
        /// <summary>
        /// Get all registered features from the AppFeatures table.
        /// </summary>
        Task<List<FeatureDto>> GetAllFeaturesAsync();

        /// <summary>
        /// Resolves the final feature map by ANDing plan features with role features.
        /// A feature is enabled only when BOTH the plan AND the role allow it.
        /// </summary>
        Task<Dictionary<string, bool>> GetResolvedFeaturesAsync(int planId, string role);

        /// <summary>
        /// Get features allowed by a specific subscription plan.
        /// </summary>
        Task<Dictionary<string, bool>> GetPlanFeaturesAsync(int planId);

        /// <summary>
        /// Get features allowed by a specific role.
        /// </summary>
        Task<Dictionary<string, bool>> GetRoleFeaturesAsync(string role);
    }

    public class FeatureDto
    {
        public int Id { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "Core";
        public bool IsActive { get; set; }
    }
}
