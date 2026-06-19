using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly BillingDbContext _context;

        public InventoryRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryItem?> GetByIdAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var results = await _context.InventoryItems
                .FromSqlRaw("EXEC dbo.sp_GetInventoryItemById @BusinessId, @Id", pBusinessId, pId)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<InventoryItem>> GetByBusinessIdAsync(int businessId)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            return await _context.InventoryItems
                .FromSqlRaw("EXEC dbo.sp_GetInventoryItemsByBusinessId @BusinessId", pBusinessId)
                .ToListAsync();
        }

        public async Task<InventoryItem> AddAsync(InventoryItem item)
        {
            var pBusinessId = new SqlParameter("@BusinessId", item.BusinessId);
            var pName = new SqlParameter("@Name", item.Name);
            var pSKU = new SqlParameter("@SKU", item.SKU);
            var pCategory = new SqlParameter("@Category", item.Category);
            var pCurrentStock = new SqlParameter("@CurrentStock", item.CurrentStock);
            var pUnit = new SqlParameter("@Unit", item.Unit);
            var pReorderLevel = new SqlParameter("@ReorderLevel", item.ReorderLevel);
            var pImageUrl = new SqlParameter("@ImageUrl", item.ImageUrl ?? (object)System.DBNull.Value);

            var results = await _context.InventoryItems
                .FromSqlRaw("EXEC dbo.sp_CreateInventoryItem @BusinessId, @Name, @SKU, @Category, @CurrentStock, @Unit, @ReorderLevel, @ImageUrl",
                    pBusinessId, pName, pSKU, pCategory, pCurrentStock, pUnit, pReorderLevel, pImageUrl)
                .ToListAsync();
            return results.First();
        }

        public async Task<InventoryItem> UpdateAsync(InventoryItem item)
        {
            var pBusinessId = new SqlParameter("@BusinessId", item.BusinessId);
            var pId = new SqlParameter("@Id", item.Id);
            var pName = new SqlParameter("@Name", item.Name);
            var pSKU = new SqlParameter("@SKU", item.SKU);
            var pCategory = new SqlParameter("@Category", item.Category);
            var pCurrentStock = new SqlParameter("@CurrentStock", item.CurrentStock);
            var pUnit = new SqlParameter("@Unit", item.Unit);
            var pReorderLevel = new SqlParameter("@ReorderLevel", item.ReorderLevel);
            var pImageUrl = new SqlParameter("@ImageUrl", item.ImageUrl ?? (object)System.DBNull.Value);

            var results = await _context.InventoryItems
                .FromSqlRaw("EXEC dbo.sp_UpdateInventoryItem @BusinessId, @Id, @Name, @SKU, @Category, @CurrentStock, @Unit, @ReorderLevel, @ImageUrl",
                    pBusinessId, pId, pName, pSKU, pCategory, pCurrentStock, pUnit, pReorderLevel, pImageUrl)
                .ToListAsync();
            return results.First();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var pBusinessId = new SqlParameter("@BusinessId", businessId);
            var pId = new SqlParameter("@Id", id);
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteInventoryItem @BusinessId, @Id", pBusinessId, pId);
            return result > 0;
        }
    }
}
