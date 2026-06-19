using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IServiceRepository
    {
        Task<Service?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<Service>> GetByBusinessIdAsync(int businessId);
        Task<Service> AddAsync(Service service);
        Task<Service> UpdateAsync(Service service);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
