using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly BillingDbContext _context;

        public CustomerRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var results = await _context.Customers
                .FromSqlRaw("EXEC dbo.sp_GetCustomerById @BusinessId, @Id", pBusinessId, pId)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Customer>> GetByBusinessIdAsync(int businessId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            return await _context.Customers
                .FromSqlRaw("EXEC dbo.sp_GetCustomersByBusinessId @BusinessId", pBusinessId)
                .ToListAsync();
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            var pBusinessId = new SqlParameter("@BusinessId", customer.BusinessId);
            var pName = new SqlParameter("@Name", customer.Name);
            var pPhone = new SqlParameter("@Phone", customer.Phone ?? (object)System.DBNull.Value);
            var pEmail = new SqlParameter("@Email", customer.Email ?? (object)System.DBNull.Value);
            var pIsWalkIn = new SqlParameter("@IsWalkIn", customer.IsWalkIn);

            var results = await _context.Customers
                .FromSqlRaw("EXEC dbo.sp_CreateCustomer @BusinessId, @Name, @Phone, @Email, @IsWalkIn",
                    pBusinessId, pName, pPhone, pEmail, pIsWalkIn)
                .ToListAsync();
            return results.First();
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            var pBusinessId = new SqlParameter("@BusinessId", customer.BusinessId);
            var pId = new SqlParameter("@Id", customer.Id);
            var pName = new SqlParameter("@Name", customer.Name);
            var pPhone = new SqlParameter("@Phone", customer.Phone ?? (object)System.DBNull.Value);
            var pEmail = new SqlParameter("@Email", customer.Email ?? (object)System.DBNull.Value);
            var pIsWalkIn = new SqlParameter("@IsWalkIn", customer.IsWalkIn);

            var results = await _context.Customers
                .FromSqlRaw("EXEC dbo.sp_UpdateCustomer @BusinessId, @Id, @Name, @Phone, @Email, @IsWalkIn",
                    pBusinessId, pId, pName, pPhone, pEmail, pIsWalkIn)
                .ToListAsync();
            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteCustomer @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
