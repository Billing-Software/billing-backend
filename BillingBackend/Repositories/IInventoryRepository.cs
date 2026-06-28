using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IInventoryRepository
    {
        Task<InventoryItem?> GetByIdAsync(int businessId, int id);
        Task<InventoryItem?> GetBySKUAsync(int businessId, string sku);
        Task<IEnumerable<InventoryItem>> GetByBusinessIdAsync(int businessId);
        Task<InventoryItem> AddAsync(InventoryItem item);
        Task<InventoryItem> UpdateAsync(InventoryItem item);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
