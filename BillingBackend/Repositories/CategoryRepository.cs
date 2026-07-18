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

            // Find all descendants recursively
            var allCategories = await _context.Categories.Where(c => c.BusinessId == businessId).ToListAsync();
            var toDelete = new List<Category> { existing };
            GetDescendants(existing.Id, allCategories, toDelete);

            _context.Categories.RemoveRange(toDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Category?> GetByIdAsync(int businessId, int id)
        {
            return await _context.Categories.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == id);
        }

        public async Task<Category?> UpdateAsync(Category category)
        {
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.BusinessId == category.BusinessId && c.Id == category.Id);
            if (existing == null)
            {
                return null;
            }

            existing.Name = category.Name;
            existing.Type = category.Type;
            existing.ParentId = category.ParentId;

            _context.Categories.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        private void GetDescendants(int parentId, List<Category> allCategories, List<Category> result)
        {
            var children = allCategories.Where(c => c.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                result.Add(child);
                GetDescendants(child.Id, allCategories, result);
            }
        }
    }
}
