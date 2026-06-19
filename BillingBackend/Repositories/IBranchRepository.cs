using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IBranchRepository
    {
        Task<Branch?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<Branch>> GetByBusinessIdAsync(int businessId);
        Task<Branch> AddAsync(Branch branch);
        Task<Branch> UpdateAsync(Branch branch);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
