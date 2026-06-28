using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly BillingDbContext _context;

        public SettingsRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<(WhatsAppSettings? Settings, IEnumerable<WhatsAppTemplate> Templates)> GetWhatsAppSettingsAsync(int businessId)
        {
            var settings = await _context.WhatsAppSettings.FirstOrDefaultAsync(w => w.BusinessId == businessId);
            IEnumerable<WhatsAppTemplate> templates = new List<WhatsAppTemplate>();
            if (settings != null)
            {
                templates = await _context.WhatsAppTemplates.Where(t => t.WhatsAppSettingsId == settings.Id).ToListAsync();
            }
            return (settings, templates);
        }

        public async Task<WhatsAppSettings> UpdateWhatsAppSettingsAsync(int businessId, string? apiKey, bool isConnected)
        {
            var settings = await _context.WhatsAppSettings.FirstOrDefaultAsync(w => w.BusinessId == businessId);
            if (settings == null)
            {
                settings = new WhatsAppSettings
                {
                    BusinessId = businessId,
                    ApiKey = apiKey,
                    IsConnected = isConnected,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.WhatsAppSettings.AddAsync(settings);
            }
            else
            {
                settings.ApiKey = apiKey;
                settings.IsConnected = isConnected;
                settings.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<WhatsAppTemplate> AddWhatsAppTemplateAsync(int businessId, string templateName)
        {
            var settings = await _context.WhatsAppSettings.FirstOrDefaultAsync(w => w.BusinessId == businessId);
            if (settings == null)
            {
                settings = new WhatsAppSettings
                {
                    BusinessId = businessId,
                    ApiKey = null,
                    IsConnected = false,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.WhatsAppSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            var template = new WhatsAppTemplate
            {
                WhatsAppSettingsId = settings.Id,
                TemplateName = templateName
            };
            await _context.WhatsAppTemplates.AddAsync(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteWhatsAppTemplateAsync(int businessId, int templateId)
        {
            var template = await _context.WhatsAppTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.WhatsAppSettings.BusinessId == businessId);
            if (template == null)
            {
                return false;
            }
            _context.WhatsAppTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
