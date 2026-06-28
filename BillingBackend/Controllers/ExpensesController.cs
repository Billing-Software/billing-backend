using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class ExpensesController : BaseApiController
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetAll()
        {
            var expenses = await _expenseService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(expenses);
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create(ExpenseDto dto)
        {
            var created = await _expenseService.AddAsync(CurrentBusinessId, dto);
            return Ok(created);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _expenseService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete expense record.");
            return NoContent();
        }
    }
}
