using BillingBackend.Data.Entities;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IBusinessRepository
    {
        Task<Business?> GetByIdAsync(int id);
        Task<Business?> GetByOwnerIdAsync(int ownerId);
        Task<Business> UpdateAsync(Business business);
    }
}
