using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseDto>> GetByBusinessIdAsync(int businessId);
        Task<ExpenseDto> AddAsync(int businessId, ExpenseDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
