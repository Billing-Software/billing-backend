using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;

namespace BillingBackend.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetByBusinessIdAsync(int businessId);
        Task<Category> AddAsync(Category category);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
