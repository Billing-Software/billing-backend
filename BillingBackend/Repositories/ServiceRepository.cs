using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
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
            return await _context.Services.FirstOrDefaultAsync(s => s.BusinessId == businessId && s.Id == id);
        }

        public async Task<Service?> GetBySKUAsync(int businessId, string sku)
        {
            return await _context.Services
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.SKU.ToLower() == sku.ToLower());
        }

        public async Task<IEnumerable<Service>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Services.Where(s => s.BusinessId == businessId).ToListAsync();
        }

        public async Task<Service> AddAsync(Service service)
        {
            await _context.Services.AddAsync(service);
            await _context.SaveChangesAsync();
            return service;
        }

        public async Task<Service> UpdateAsync(Service service)
        {
            var existing = await _context.Services.FirstOrDefaultAsync(s => s.BusinessId == service.BusinessId && s.Id == service.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Service with ID {service.Id} for Business {service.BusinessId} not found");
            }

            existing.Name = service.Name;
            existing.SKU = service.SKU;
            existing.Category = service.Category;
            existing.BasePrice = service.BasePrice;
            existing.TaxRate = service.TaxRate;
            existing.Status = service.Status;
            existing.IconName = service.IconName;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Services.FirstOrDefaultAsync(s => s.BusinessId == businessId && s.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Services.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
