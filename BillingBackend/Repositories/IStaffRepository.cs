using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IStaffRepository
    {
        Task<StaffMember?> GetByIdAsync(int businessId, int id);
        Task<StaffMember?> GetByUserIdAsync(int userId);
        Task<StaffMember?> GetByUserIdAndBusinessIdAsync(int userId, int businessId);
        Task<IEnumerable<StaffMember>> GetByBusinessIdAsync(int businessId);
        Task<StaffMember> AddAsync(StaffMember staff, string password);
        Task<StaffMember> UpdateAsync(StaffMember staff, string? password);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
