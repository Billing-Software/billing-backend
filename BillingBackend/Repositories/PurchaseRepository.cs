using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly BillingDbContext _context;

        public PurchaseRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Purchase>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Purchases
                .Where(p => p.BusinessId == businessId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        public async Task<Purchase?> GetByIdAsync(int businessId, int id)
        {
            return await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.BusinessId == businessId && p.Id == id);
        }

        public async Task<Purchase> AddAsync(Purchase purchase)
        {
            purchase.CreatedAt = DateTime.UtcNow;
            if (purchase.PurchaseDate == default)
            {
                purchase.PurchaseDate = DateTime.UtcNow;
            }

            await _context.Purchases.AddAsync(purchase);
            await _context.SaveChangesAsync();
            return purchase;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Purchases.FirstOrDefaultAsync(p => p.BusinessId == businessId && p.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Purchases.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
