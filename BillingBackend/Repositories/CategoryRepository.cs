using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BillingDbContext _context;

        public CategoryRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Categories.Where(c => c.BusinessId == businessId).ToListAsync();
        }

        public async Task<Category> AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Categories.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
