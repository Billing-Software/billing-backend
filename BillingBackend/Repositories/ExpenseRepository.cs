using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly BillingDbContext _context;

        public ExpenseRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Expense>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.Expenses.Where(e => e.BusinessId == businessId).ToListAsync();
        }

        public async Task<Expense> AddAsync(Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
            return expense;
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.BusinessId == businessId && e.Id == id);
            if (existing == null)
            {
                return false;
            }

            _context.Expenses.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
