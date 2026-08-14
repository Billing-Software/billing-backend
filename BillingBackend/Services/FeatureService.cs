using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BillingBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class FeatureService : IFeatureService
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<FeatureService> _logger;

        public FeatureService(BillingDbContext context, ILogger<FeatureService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FeatureDto>> GetAllFeaturesAsync()
        {
            return await _context.AppFeatures
                .Where(f => f.IsActive)
                .OrderBy(f => f.Id)
                .Select(f => new FeatureDto
                {
                    Id = f.Id,
                    FeatureKey = f.FeatureKey,
                    DisplayName = f.DisplayName,
                    Description = f.Description,
                    Category = f.Category,
                    IsActive = f.IsActive
                })
                .ToListAsync();
        }

        public async Task<Dictionary<string, bool>> GetPlanFeaturesAsync(int planId)
        {
            var planFeatures = await _context.PlanFeatures
                .Include(pf => pf.Feature)
                .Where(pf => pf.PlanId == planId && pf.Feature.IsActive)
                .ToListAsync();

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var pf in planFeatures)
            {
                result[pf.Feature.FeatureKey] = pf.IsEnabled;
            }
            return result;
        }

        public async Task<Dictionary<string, bool>> GetRoleFeaturesAsync(string role)
        {
            var roleFeatures = await _context.RoleFeatures
                .Include(rf => rf.Feature)
                .Where(rf => rf.RoleName == role && rf.Feature.IsActive)
                .ToListAsync();

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var rf in roleFeatures)
            {
                result[rf.Feature.FeatureKey] = rf.IsEnabled;
            }
            return result;
        }

        public async Task<Dictionary<string, bool>> GetResolvedFeaturesAsync(int planId, string role)
        {
            _logger.LogDebug("[FeatureService] Resolving features for PlanId={PlanId}, Role={Role}", planId, role);

            var planFeatures = await GetPlanFeaturesAsync(planId);
            var roleFeatures = await GetRoleFeaturesAsync(role);

            // Get all active feature keys as the base set
            var allFeatureKeys = await _context.AppFeatures
                .Where(f => f.IsActive)
                .Select(f => f.FeatureKey)
                .ToListAsync();

            var resolved = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in allFeatureKeys)
            {
                // A feature is accessible only when BOTH plan AND role allow it
                bool planAllows = planFeatures.TryGetValue(key, out var pv) && pv;
                bool roleAllows = roleFeatures.TryGetValue(key, out var rv) && rv;
                resolved[key] = planAllows && roleAllows;
            }

            _logger.LogDebug("[FeatureService] Resolved {Count} features: {Features}",
                resolved.Count,
                string.Join(", ", resolved.Where(kv => kv.Value).Select(kv => kv.Key)));

            return resolved;
        }
    }
}
