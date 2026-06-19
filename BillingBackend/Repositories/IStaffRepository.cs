using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IStaffRepository
    {
        Task<StaffMember?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<StaffMember>> GetByBusinessIdAsync(int businessId);
        Task<StaffMember> AddAsync(StaffMember staff);
        Task<StaffMember> UpdateAsync(StaffMember staff);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
