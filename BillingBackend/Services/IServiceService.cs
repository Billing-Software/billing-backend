using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IServiceService
    {
        Task<ServiceDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<ServiceDto>> GetByBusinessIdAsync(int businessId);
        Task<ServiceDto> AddAsync(int businessId, ServiceDto dto);
        Task<ServiceDto> UpdateAsync(int businessId, ServiceDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
