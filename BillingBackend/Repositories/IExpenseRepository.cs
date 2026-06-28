using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;

namespace BillingBackend.Repositories
{
    public interface IExpenseRepository
    {
        Task<IEnumerable<Expense>> GetByBusinessIdAsync(int businessId);
        Task<Expense> AddAsync(Expense expense);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
