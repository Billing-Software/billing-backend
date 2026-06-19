using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IBillRepository
    {
        Task<Bill?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<Bill>> GetByBusinessIdAsync(int businessId);
        Task<Bill> AddAsync(Bill bill, string itemsJson);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
