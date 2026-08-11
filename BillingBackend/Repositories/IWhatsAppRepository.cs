using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IWhatsAppRepository
    {
        Task<WhatsAppAccount?> GetByBusinessIdAsync(int businessId);
        Task<WhatsAppAccount> CreateOrUpdateAsync(WhatsAppAccount account);
        Task<bool> DeleteByBusinessIdAsync(int businessId);
        Task<MessageLog> AddMessageLogAsync(MessageLog log);
        Task<MessageLog?> GetMessageLogByMetaIdAsync(string metaMessageId);
        Task<IEnumerable<MessageLog>> GetMessageLogsByBusinessAsync(int businessId, int? billId = null);
        Task UpdateMessageLogStatusAsync(string metaMessageId, string status, System.DateTime? timestamp, string? failedReason);
        Task<WhatsAppTemplate?> GetTemplateByNameAsync(int whatsAppAccountId, string templateName);
        Task<IEnumerable<WhatsAppTemplate>> GetTemplatesByAccountAsync(int whatsAppAccountId);
        Task<WhatsAppTemplate> SaveTemplateAsync(WhatsAppTemplate template);
    }
}
