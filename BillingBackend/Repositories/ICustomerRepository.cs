using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<Customer>> GetByBusinessIdAsync(int businessId);
        Task<Customer> AddAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
