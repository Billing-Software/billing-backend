using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly BillingDbContext _context;

        public InventoryRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryItem?> GetByIdAsync(int businessId, int id)
        {
            return await _context.InventoryItems.FirstOrDefaultAsync(i => i.BusinessId == businessId && i.Id == id);
        }

        public async Task<InventoryItem?> GetBySKUAsync(int businessId, string sku)
        {
            return await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.BusinessId == businessId && i.SKU.ToLower() == sku.ToLower());
        }

        public async Task<IEnumerable<InventoryItem>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.InventoryItems.Where(i => i.BusinessId == businessId).ToListAsync();
        }

        public async Task<InventoryItem> AddAsync(InventoryItem item)
        {
            await _context.InventoryItems.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<InventoryItem> UpdateAsync(InventoryItem item)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.BusinessId == item.BusinessId && i.Id == item.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Inventory item with ID {item.Id} for Business {item.BusinessId} not found");
            }

            existing.Name = item.Name;
            existing.SKU = item.SKU;
            existing.Category = item.Category;
            existing.CurrentStock = item.CurrentStock;
            existing.Unit = item.Unit;
            existing.ReorderLevel = item.ReorderLevel;
            existing.ImageUrl = item.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            // Compare with the version read by the caller, rather than silently overwriting a newer stock value.
            _context.Entry(existing).Property(x => x.RowVersion).OriginalValue = item.RowVersion;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.BusinessId == businessId && i.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.InventoryItems.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
