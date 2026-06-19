using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class BillsController : BaseApiController
    {
        private readonly IBillService _billService;

        public BillsController(IBillService billService)
        {
            _billService = billService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetAll()
        {
            var bills = await _billService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(bills);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetById(int id)
        {
            var bill = await _billService.GetByIdAsync(CurrentBusinessId, id);
            if (bill == null) return NotFound("Bill not found.");
            return Ok(bill);
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> Create(CreateBillDto dto)
        {
            var created = await _billService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _billService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete bill.");
            return NoContent();
        }
    }
}
