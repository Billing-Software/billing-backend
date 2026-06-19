using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IStaffService
    {
        Task<StaffDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<StaffDto>> GetByBusinessIdAsync(int businessId);
        Task<StaffDto> AddAsync(int businessId, StaffDto dto);
        Task<StaffDto> UpdateAsync(int businessId, StaffDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
