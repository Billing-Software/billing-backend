using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly BillingDbContext _context;

        public ServiceRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Service?> GetByIdAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var results = await _context.Services
                .FromSqlRaw("EXEC dbo.sp_GetServiceById @BusinessId, @Id", pBusinessId, pId)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Service>> GetByBusinessIdAsync(int businessId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            return await _context.Services
                .FromSqlRaw("EXEC dbo.sp_GetServicesByBusinessId @BusinessId", pBusinessId)
                .ToListAsync();
        }

        public async Task<Service> AddAsync(Service service)
        {
            var pBusinessId = new SqlParameter("@BusinessId", service.BusinessId);
            var pName = new SqlParameter("@Name", service.Name);
            var pSKU = new SqlParameter("@SKU", service.SKU);
            var pCategory = new SqlParameter("@Category", service.Category);
            var pBasePrice = new SqlParameter("@BasePrice", service.BasePrice);
            var pTaxRate = new SqlParameter("@TaxRate", service.TaxRate);
            var pStatus = new SqlParameter("@Status", service.Status);
            var pIconName = new SqlParameter("@IconName", service.IconName ?? (object)System.DBNull.Value);

            var results = await _context.Services
                .FromSqlRaw("EXEC dbo.sp_CreateService @BusinessId, @Name, @SKU, @Category, @BasePrice, @TaxRate, @Status, @IconName",
                    pBusinessId, pName, pSKU, pCategory, pBasePrice, pTaxRate, pStatus, pIconName)
                .ToListAsync();
            return results.First();
        }

        public async Task<Service> UpdateAsync(Service service)
        {
            var pBusinessId = new SqlParameter("@BusinessId", service.BusinessId);
            var pId = new SqlParameter("@Id", service.Id);
            var pName = new SqlParameter("@Name", service.Name);
            var pSKU = new SqlParameter("@SKU", service.SKU);
            var pCategory = new SqlParameter("@Category", service.Category);
            var pBasePrice = new SqlParameter("@BasePrice", service.BasePrice);
            var pTaxRate = new SqlParameter("@TaxRate", service.TaxRate);
            var pStatus = new SqlParameter("@Status", service.Status);
            var pIconName = new SqlParameter("@IconName", service.IconName ?? (object)System.DBNull.Value);

            var results = await _context.Services
                .FromSqlRaw("EXEC dbo.sp_UpdateService @BusinessId, @Id, @Name, @SKU, @Category, @BasePrice, @TaxRate, @Status, @IconName",
                    pBusinessId, pId, pName, pSKU, pCategory, pBasePrice, pTaxRate, pStatus, pIconName)
                .ToListAsync();
            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteService @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
