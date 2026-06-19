using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IBranchService
    {
        Task<BranchDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<BranchDto>> GetByBusinessIdAsync(int businessId);
        Task<BranchDto> AddAsync(int businessId, BranchDto dto);
        Task<BranchDto> UpdateAsync(int businessId, BranchDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
