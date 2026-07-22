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
        public async Task<ActionResult<IEnumerable<BillDto>>> GetAll(
            [FromQuery] int? customerId = null,
            [FromQuery] int? staffId = null,
            [FromQuery] int? branchId = null,
            [FromQuery] System.DateTime? startDate = null,
            [FromQuery] System.DateTime? endDate = null,
            [FromQuery] string? status = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null)
        {
            try
            {
                var bills = await _billService.GetByBusinessIdAsync(
                    CurrentBusinessId, customerId, staffId, branchId, startDate, endDate, status, minAmount, maxAmount);
                return Ok(bills);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching bills.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetById(int id)
        {
            try
            {
                var bill = await _billService.GetByIdAsync(CurrentBusinessId, id);
                if (bill == null) return NotFound("Bill not found.");
                return Ok(bill);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching bill details.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> Create(CreateBillDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.IdempotencyKey) && Request.Headers.TryGetValue("X-Idempotency-Key", out var key))
                {
                    dto.IdempotencyKey = key.ToString();
                }

                if (!string.IsNullOrEmpty(dto.IdempotencyKey))
                {
                    var existing = await _billService.GetByIdempotencyKeyAsync(CurrentBusinessId, dto.IdempotencyKey);
                    if (existing != null)
                    {
                        Response.Headers["X-Cache-Lookup"] = "HIT - Idempotency Key";
                        return Ok(existing);
                    }
                }

                var created = await _billService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating bill.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _billService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete bill.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting bill.", error = ex.Message });
            }
        }
    }
}
