using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly BillingDbContext _context;

        public StaffRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<StaffMember?> GetByIdAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var results = await _context.StaffMembers
                .FromSqlRaw("EXEC dbo.sp_GetStaffMemberById @BusinessId, @Id", pBusinessId, pId)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<StaffMember>> GetByBusinessIdAsync(int businessId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            return await _context.StaffMembers
                .FromSqlRaw("EXEC dbo.sp_GetStaffMembersByBusinessId @BusinessId", pBusinessId)
                .ToListAsync();
        }

        public async Task<StaffMember> AddAsync(StaffMember staff)
        {
            var pBusinessId = new SqlParameter("@BusinessId", staff.BusinessId);
            var pUserId = new SqlParameter("@UserId", staff.UserId ?? (object)System.DBNull.Value);
            var pName = new SqlParameter("@Name", staff.Name);
            var pEmpCode = new SqlParameter("@EmpCode", staff.EmpCode);
            var pContact = new SqlParameter("@Contact", staff.Contact ?? (object)System.DBNull.Value);
            var pRole = new SqlParameter("@Role", staff.Role);
            var pStatus = new SqlParameter("@Status", staff.Status);

            var results = await _context.StaffMembers
                .FromSqlRaw("EXEC dbo.sp_CreateStaffMember @BusinessId, @UserId, @Name, @EmpCode, @Contact, @Role, @Status",
                    pBusinessId, pUserId, pName, pEmpCode, pContact, pRole, pStatus)
                .ToListAsync();
            return results.First();
        }

        public async Task<StaffMember> UpdateAsync(StaffMember staff)
        {
            var pBusinessId = new SqlParameter("@BusinessId", staff.BusinessId);
            var pId = new SqlParameter("@Id", staff.Id);
            var pUserId = new SqlParameter("@UserId", staff.UserId ?? (object)System.DBNull.Value);
            var pName = new SqlParameter("@Name", staff.Name);
            var pEmpCode = new SqlParameter("@EmpCode", staff.EmpCode);
            var pContact = new SqlParameter("@Contact", staff.Contact ?? (object)System.DBNull.Value);
            var pRole = new SqlParameter("@Role", staff.Role);
            var pStatus = new SqlParameter("@Status", staff.Status);

            var results = await _context.StaffMembers
                .FromSqlRaw("EXEC dbo.sp_UpdateStaffMember @BusinessId, @Id, @UserId, @Name, @EmpCode, @Contact, @Role, @Status",
                    pBusinessId, pId, pUserId, pName, pEmpCode, pContact, pRole, pStatus)
                .ToListAsync();
            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteStaffMember @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
