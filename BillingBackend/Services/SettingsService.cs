using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository;

        public SettingsService(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<WhatsAppSettingsDto> GetWhatsAppSettingsAsync(int businessId)
        {
            var (settings, templates) = await _settingsRepository.GetWhatsAppSettingsAsync(businessId);
            
            var dto = new WhatsAppSettingsDto
            {
                Id = settings?.Id ?? 0,
                BusinessId = businessId,
                ApiKey = settings?.ApiKey,
                IsConnected = settings?.IsConnected ?? false,
                Templates = templates.Select(t => new WhatsAppTemplateDto
                {
                    Id = t.Id,
                    WhatsAppSettingsId = t.WhatsAppSettingsId,
                    TemplateName = t.TemplateName
                }).ToList()
            };

            return dto;
        }

        public async Task<WhatsAppSettingsDto> UpdateWhatsAppSettingsAsync(int businessId, UpdateWhatsAppSettingsDto dto)
        {
            var updated = await _settingsRepository.UpdateWhatsAppSettingsAsync(businessId, dto.ApiKey, dto.IsConnected);
            return await GetWhatsAppSettingsAsync(businessId);
        }

        public async Task<WhatsAppTemplateDto> AddWhatsAppTemplateAsync(int businessId, AddWhatsAppTemplateDto dto)
        {
            var template = await _settingsRepository.AddWhatsAppTemplateAsync(businessId, dto.TemplateName);
            return new WhatsAppTemplateDto
            {
                Id = template.Id,
                WhatsAppSettingsId = template.WhatsAppSettingsId,
                TemplateName = template.TemplateName
            };
        }

        public async Task<bool> DeleteWhatsAppTemplateAsync(int businessId, int templateId)
        {
            return await _settingsRepository.DeleteWhatsAppTemplateAsync(businessId, templateId);
        }
    }
}
