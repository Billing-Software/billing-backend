using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;

        public ExpenseService(IExpenseRepository expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        public async Task<IEnumerable<ExpenseDto>> GetByBusinessIdAsync(int businessId)
        {
            var list = await _expenseRepository.GetByBusinessIdAsync(businessId);
            return list.Select(MapToDto);
        }

        public async Task<ExpenseDto> AddAsync(int businessId, ExpenseDto dto)
        {
            var expense = new Expense
            {
                BusinessId = businessId,
                Description = dto.Description,
                Amount = dto.Amount,
                Category = dto.Category,
                ExpenseDate = dto.ExpenseDate == default ? DateTime.UtcNow : dto.ExpenseDate
            };

            var added = await _expenseRepository.AddAsync(expense);
            return MapToDto(added);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _expenseRepository.DeleteAsync(businessId, id);
        }

        private ExpenseDto MapToDto(Expense e)
        {
            return new ExpenseDto
            {
                Id = e.Id,
                BusinessId = e.BusinessId,
                Description = e.Description,
                Amount = e.Amount,
                Category = e.Category,
                ExpenseDate = e.ExpenseDate,
                CreatedAt = e.CreatedAt
            };
        }
    }
}
