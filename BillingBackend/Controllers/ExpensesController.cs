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
            try
            {
                var expenses = await _expenseService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(expenses);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching expenses.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create(ExpenseDto dto)
        {
            try
            {
                var created = await _expenseService.AddAsync(CurrentBusinessId, dto);
                return Ok(created);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating expense record.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _expenseService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete expense record.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting expense record.", error = ex.Message });
            }
        }
    }
}
