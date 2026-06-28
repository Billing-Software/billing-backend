using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;

namespace BillingBackend.Repositories
{
    public interface IPurchaseRepository
    {
        Task<IEnumerable<Purchase>> GetByBusinessIdAsync(int businessId);
        Task<Purchase?> GetByIdAsync(int businessId, int id);
        Task<Purchase> AddAsync(Purchase purchase);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
