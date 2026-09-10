using System;
using System.Threading.Tasks;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingBackend.Data;

namespace BillingBackend.Controllers
{
    public class FeatureController : BaseApiController
    {
        private readonly IFeatureService _featureService;
        private readonly BillingDbContext _context;

        public FeatureController(IFeatureService featureService, BillingDbContext context)
        {
            _featureService = featureService;
            _context = context;
        }

        /// <summary>
        /// Returns the resolved feature map for the currently authenticated user.
        /// Features are resolved by ANDing the user's subscription plan features
        /// with their role features.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyFeatures()
        {
            try
            {
                var businessId = CurrentBusinessId;
                var role = CurrentUserRole;

                // Look up the business's active plan
                var business = await _context.Businesses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == businessId);

                if (business == null)
                {
                    return NotFound(new { message = "Business not found." });
                }

                var planId = business.ActivePlanId;
                var features = await _featureService.GetResolvedFeaturesAsync(planId, role);

                return Ok(new
                {
                    planId,
                    role,
                    features
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resolving features.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        /// <summary>
        /// Returns the master list of all registered features (SuperAdmin only).
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllFeatures()
        {
            try
            {
                var features = await _featureService.GetAllFeaturesAsync();
                return Ok(features);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving features.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
