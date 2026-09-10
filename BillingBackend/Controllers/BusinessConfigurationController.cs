using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    public class BusinessConfigurationController : BaseApiController
    {
        private readonly IBusinessConfigurationService _configService;

        public BusinessConfigurationController(IBusinessConfigurationService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Get available business type presets & search aliases (Anonymous access allowed for onboarding UI).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<BusinessTypePresetDto>>> GetBusinessTypes()
        {
            var presets = await _configService.GetBusinessTypePresetsAsync();
            return Ok(presets);
        }

        /// <summary>
        /// Get configuration & vocabulary for the authenticated business.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BusinessConfigDto>> GetConfiguration()
        {
            try
            {
                var config = await _configService.GetConfigurationAsync(CurrentBusinessId, CurrentUserRole);
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving business configuration.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        /// <summary>
        /// Update configuration, business type, or custom terminology overrides.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<BusinessConfigDto>> UpdateConfiguration([FromBody] UpdateBusinessConfigDto dto)
        {
            try
            {
                var updated = await _configService.UpdateConfigurationAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating business configuration.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
