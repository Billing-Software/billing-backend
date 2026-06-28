using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
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
            return await _context.Branches.FirstOrDefaultAsync(b => b.BusinessId == businessId && b.Id == id);
        }

        public async Task<IEnumerable<Branch>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Branches.Where(b => b.BusinessId == businessId).ToListAsync();
        }

        public async Task<Branch> AddAsync(Branch branch)
        {
            await _context.Branches.AddAsync(branch);
            await _context.SaveChangesAsync();
            return branch;
        }

        public async Task<Branch> UpdateAsync(Branch branch)
        {
            var existing = await _context.Branches.FirstOrDefaultAsync(b => b.BusinessId == branch.BusinessId && b.Id == branch.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Branch with ID {branch.Id} for Business {branch.BusinessId} not found");
            }

            existing.Name = branch.Name;
            existing.Address = branch.Address;
            existing.City = branch.City;
            existing.PostalCode = branch.PostalCode;
            existing.Phone = branch.Phone;
            existing.IsActive = branch.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Branches.FirstOrDefaultAsync(b => b.BusinessId == businessId && b.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Branches.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
