using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly BillingDbContext _context;

        public BranchRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Branch?> GetByIdAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var results = await _context.Branches
                .FromSqlRaw("EXEC dbo.sp_GetBranchById @BusinessId, @Id", pBusinessId, pId)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Branch>> GetByBusinessIdAsync(int businessId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            return await _context.Branches
                .FromSqlRaw("EXEC dbo.sp_GetBranchesByBusinessId @BusinessId", pBusinessId)
                .ToListAsync();
        }

        public async Task<Branch> AddAsync(Branch branch)
        {
            var pBusinessId = new SqlParameter("@BusinessId", branch.BusinessId);
            var pName = new SqlParameter("@Name", branch.Name);
            var pAddress = new SqlParameter("@Address", branch.Address ?? (object)System.DBNull.Value);
            var pCity = new SqlParameter("@City", branch.City ?? (object)System.DBNull.Value);
            var pPostalCode = new SqlParameter("@PostalCode", branch.PostalCode ?? (object)System.DBNull.Value);
            var pPhone = new SqlParameter("@Phone", branch.Phone ?? (object)System.DBNull.Value);
            var pIsActive = new SqlParameter("@IsActive", branch.IsActive);

            var results = await _context.Branches
                .FromSqlRaw("EXEC dbo.sp_CreateBranch @BusinessId, @Name, @Address, @City, @PostalCode, @Phone, @IsActive",
                    pBusinessId, pName, pAddress, pCity, pPostalCode, pPhone, pIsActive)
                .ToListAsync();
            return results.First();
        }

        public async Task<Branch> UpdateAsync(Branch branch)
        {
            var pBusinessId = new SqlParameter("@BusinessId", branch.BusinessId);
            var pId = new SqlParameter("@Id", branch.Id);
            var pName = new SqlParameter("@Name", branch.Name);
            var pAddress = new SqlParameter("@Address", branch.Address ?? (object)System.DBNull.Value);
            var pCity = new SqlParameter("@City", branch.City ?? (object)System.DBNull.Value);
            var pPostalCode = new SqlParameter("@PostalCode", branch.PostalCode ?? (object)System.DBNull.Value);
            var pPhone = new SqlParameter("@Phone", branch.Phone ?? (object)System.DBNull.Value);
            var pIsActive = new SqlParameter("@IsActive", branch.IsActive);

            var results = await _context.Branches
                .FromSqlRaw("EXEC dbo.sp_UpdateBranch @BusinessId, @Id, @Name, @Address, @City, @PostalCode, @Phone, @IsActive",
                    pBusinessId, pId, pName, pAddress, pCity, pPostalCode, pPhone, pIsActive)
                .ToListAsync();
            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            
            // ExecuteSqlRawAsync returns the number of state-changing rows affected, or we can just run the exec
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteBranch @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
