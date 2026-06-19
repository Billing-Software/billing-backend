using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class SettingsController : BaseApiController
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet("whatsapp")]
        public async Task<ActionResult<WhatsAppSettingsDto>> GetWhatsAppSettings()
        {
            var settings = await _settingsService.GetWhatsAppSettingsAsync(CurrentBusinessId);
            return Ok(settings);
        }

        [HttpPut("whatsapp")]
        public async Task<ActionResult<WhatsAppSettingsDto>> UpdateWhatsAppSettings(UpdateWhatsAppSettingsDto dto)
        {
            var updated = await _settingsService.UpdateWhatsAppSettingsAsync(CurrentBusinessId, dto);
            return Ok(updated);
        }

        [HttpPost("whatsapp/templates")]
        public async Task<ActionResult<WhatsAppTemplateDto>> AddWhatsAppTemplate(AddWhatsAppTemplateDto dto)
        {
            var template = await _settingsService.AddWhatsAppTemplateAsync(CurrentBusinessId, dto);
            return Ok(template);
        }

        [HttpDelete("whatsapp/templates/{id}")]
        public async Task<ActionResult> DeleteWhatsAppTemplate(int id)
        {
            var deleted = await _settingsService.DeleteWhatsAppTemplateAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete template.");
            return NoContent();
        }
    }
}
