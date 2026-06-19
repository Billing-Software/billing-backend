using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly BillingDbContext _context;

        public BusinessRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Business?> GetByIdAsync(int id)
        {
            var idParam = new SqlParameter("@BusinessId", id);
            var results = await _context.Businesses
                .FromSqlRaw("EXEC dbo.sp_GetBusinessProfile @BusinessId", idParam)
                .ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<Business?> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == ownerId);
        }

        public async Task<Business> UpdateAsync(Business business)
        {
            var pBusinessId = new SqlParameter("@BusinessId", business.Id);
            var pLegalName = new SqlParameter("@LegalName", business.LegalName);
            var pTradingName = new SqlParameter("@TradingName", business.TradingName ?? (object)System.DBNull.Value);
            var pLogoUrl = new SqlParameter("@LogoUrl", business.LogoUrl ?? (object)System.DBNull.Value);
            var pAddress = new SqlParameter("@Address", business.Address ?? (object)System.DBNull.Value);
            var pCity = new SqlParameter("@City", business.City ?? (object)System.DBNull.Value);
            var pState = new SqlParameter("@State", business.State ?? (object)System.DBNull.Value);
            var pPostalCode = new SqlParameter("@PostalCode", business.PostalCode ?? (object)System.DBNull.Value);
            var pCountry = new SqlParameter("@Country", business.Country ?? (object)System.DBNull.Value);
            var pPhone = new SqlParameter("@Phone", business.Phone ?? (object)System.DBNull.Value);
            var pEmail = new SqlParameter("@Email", business.Email ?? (object)System.DBNull.Value);
            var pWebsite = new SqlParameter("@Website", business.Website ?? (object)System.DBNull.Value);
            var pGstIn = new SqlParameter("@GstIn", business.GstIn ?? (object)System.DBNull.Value);
            var pDefaultTaxRate = new SqlParameter("@DefaultTaxRate", business.DefaultTaxRate);
            var pPricesIncludeTax = new SqlParameter("@PricesIncludeTax", business.PricesIncludeTax);

            var results = await _context.Businesses
                .FromSqlRaw("EXEC dbo.sp_UpdateBusinessProfile @BusinessId, @LegalName, @TradingName, @LogoUrl, @Address, @City, @State, @PostalCode, @Country, @Phone, @Email, @Website, @GstIn, @DefaultTaxRate, @PricesIncludeTax",
                    pBusinessId, pLegalName, pTradingName, pLogoUrl, pAddress, pCity, pState, pPostalCode, pCountry, pPhone, pEmail, pWebsite, pGstIn, pDefaultTaxRate, pPricesIncludeTax)
                .ToListAsync();

            return results.First();
        }
    }
}
