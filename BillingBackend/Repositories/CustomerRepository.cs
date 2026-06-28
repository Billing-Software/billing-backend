using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
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
            return await _context.Customers.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == id);
        }

        public async Task<IEnumerable<Customer>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Customers.Where(c => c.BusinessId == businessId).ToListAsync();
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.BusinessId == customer.BusinessId && c.Id == customer.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customer.Id} for Business {customer.BusinessId} not found");
            }

            existing.Name = customer.Name;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.IsWalkIn = customer.IsWalkIn;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Customers.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
