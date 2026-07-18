using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class WhatsAppRepository : IWhatsAppRepository
    {
        private readonly BillingDbContext _context;

        public WhatsAppRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<WhatsAppAccount?> GetByBusinessIdAsync(int businessId)
        {
            return await _context.WhatsAppAccounts
                .FirstOrDefaultAsync(w => w.BusinessId == businessId);
        }

        public async Task<WhatsAppAccount> CreateOrUpdateAsync(WhatsAppAccount account)
        {
            var existing = await _context.WhatsAppAccounts
                .FirstOrDefaultAsync(w => w.BusinessId == account.BusinessId);

            if (existing == null)
            {
                await _context.WhatsAppAccounts.AddAsync(account);
            }
            else
            {
                existing.MetaBusinessId = account.MetaBusinessId;
                existing.WabaId = account.WabaId;
                existing.PhoneNumberId = account.PhoneNumberId;
                existing.DisplayPhoneNumber = account.DisplayPhoneNumber;
                existing.AccessToken = account.AccessToken;
                existing.TokenExpiry = account.TokenExpiry;
                existing.Status = account.Status;
                existing.ConnectedAt = account.ConnectedAt;
                existing.DisconnectedAt = account.DisconnectedAt;
            }

            await _context.SaveChangesAsync();
            return existing ?? account;
        }

        public async Task<bool> DeleteByBusinessIdAsync(int businessId)
        {
            var account = await _context.WhatsAppAccounts
                .FirstOrDefaultAsync(w => w.BusinessId == businessId);

            if (account == null) return false;

            // Soft-disconnect: mark as disconnected and clear sensitive data
            account.Status = "Disconnected";
            account.AccessToken = null;
            account.DisconnectedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MessageLog> AddMessageLogAsync(MessageLog log)
        {
            await _context.MessageLogs.AddAsync(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<MessageLog?> GetMessageLogByMetaIdAsync(string metaMessageId)
        {
            return await _context.MessageLogs
                .FirstOrDefaultAsync(m => m.MetaMessageId == metaMessageId);
        }

        public async Task<IEnumerable<MessageLog>> GetMessageLogsByBusinessAsync(int businessId, int? billId = null)
        {
            var query = _context.MessageLogs
                .Include(m => m.WhatsAppAccount)
                .Where(m => m.WhatsAppAccount.BusinessId == businessId);

            if (billId.HasValue)
            {
                query = query.Where(m => m.BillId == billId.Value);
            }

            return await query
                .OrderByDescending(m => m.SentAt)
                .Take(100) // Limit to last 100 messages
                .ToListAsync();
        }

        public async Task UpdateMessageLogStatusAsync(
            string metaMessageId, string status, DateTime? timestamp, string? failedReason)
        {
            var log = await _context.MessageLogs
                .FirstOrDefaultAsync(m => m.MetaMessageId == metaMessageId);

            if (log == null) return;

            log.Status = status;

            switch (status)
            {
                case "Delivered":
                    log.DeliveredAt = timestamp ?? DateTime.UtcNow;
                    break;
                case "Read":
                    log.ReadAt = timestamp ?? DateTime.UtcNow;
                    // Also set DeliveredAt if not already set
                    log.DeliveredAt ??= timestamp ?? DateTime.UtcNow;
                    break;
                case "Failed":
                    log.FailedReason = failedReason;
                    break;
            }

            await _context.SaveChangesAsync();
        }
    }
}
