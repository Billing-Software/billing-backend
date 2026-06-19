using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface ICustomerService
    {
        Task<CustomerDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<CustomerDto>> GetByBusinessIdAsync(int businessId);
        Task<CustomerDto> AddAsync(int businessId, CustomerDto dto);
        Task<CustomerDto> UpdateAsync(int businessId, CustomerDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
