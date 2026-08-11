using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            return await _context.Businesses.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Business?> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == ownerId);
        }

        public async Task<Business> UpdateAsync(Business business)
        {
            var existing = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == business.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Business with ID {business.Id} not found");
            }

            existing.LegalName = business.LegalName;
            existing.TradingName = business.TradingName;
            existing.LogoUrl = business.LogoUrl;
            existing.Address = business.Address;
            existing.City = business.City;
            existing.State = business.State;
            existing.PostalCode = business.PostalCode;
            existing.Country = business.Country;
            existing.Phone = business.Phone;
            existing.Email = business.Email;
            existing.Website = business.Website;
            existing.GstIn = business.GstIn;
            existing.GstScheme = business.GstScheme;
            existing.DefaultTaxRate = business.DefaultTaxRate;
            existing.PricesIncludeTax = business.PricesIncludeTax;
            existing.ReceiptHeader = business.ReceiptHeader;
            existing.ReceiptFooter = business.ReceiptFooter;
            existing.ShowLogoOnReceipt = business.ShowLogoOnReceipt;
            existing.ReceiptTemplateType = business.ReceiptTemplateType;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
