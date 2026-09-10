using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class BillsController : BaseApiController
    {
        private readonly IBillService _billService;
        private readonly ILogger<BillsController> _logger;

        public BillsController(IBillService billService, ILogger<BillsController> logger)
        {
            _billService = billService;
            _logger = logger;
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
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var bills = await _billService.GetByBusinessIdAsync(
                    CurrentBusinessId, customerId, staffId, branchId, startDate, endDate, status, minAmount, maxAmount);
                return Ok(bills);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetAll bills failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error fetching bills.", correlationId = CorrelationId });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetById(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (id <= 0) return BadRequest(new { message = "Invalid bill id." });
            try
            {
                var bill = await _billService.GetByIdAsync(CurrentBusinessId, id);
                if (bill == null) return NotFound("Bill not found.");
                return Ok(bill);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetById bill failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error fetching bill details.", correlationId = CorrelationId });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> Create(CreateBillDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (!ModelState.IsValid) return BadRequest(ModelState);
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
                _logger.LogError(ex, "Create bill failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error creating bill.", correlationId = CorrelationId });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (id <= 0) return BadRequest(new { message = "Invalid bill id." });
            try
            {
                var deleted = await _billService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete bill.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Delete bill failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error deleting bill.", correlationId = CorrelationId });
            }
        }

        /// <summary>
        /// Generates the dynamic NPCI standard UPI QR payload for a bill.
        /// Client can display QR on screen or print it on receipts.
        /// </summary>
        [HttpGet("{id}/upi-qr")]
        public async Task<ActionResult<BillUpiQrResponseDto>> GetUpiQr(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (id <= 0) return BadRequest(new { message = "Invalid bill id." });
            try
            {
                var upiData = await _billService.GenerateUpiQrAsync(CurrentBusinessId, id);
                if (upiData == null) return NotFound("Bill not found.");
                return Ok(upiData);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetUpiQr failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error generating UPI QR for bill.", correlationId = CorrelationId });
            }
        }

        /// <summary>
        /// Record a payment (e.g. UPI, Cash, Card) for a bill and mark status as Paid.
        /// </summary>
        [HttpPost("{id}/payment")]
        public async Task<ActionResult<BillDto>> RecordPayment(int id, [FromBody] RecordBillPaymentDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (id <= 0) return BadRequest(new { message = "Invalid bill id." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updated = await _billService.RecordPaymentAsync(CurrentBusinessId, id, dto);
                if (updated == null) return NotFound("Bill not found.");
                return Ok(updated);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RecordPayment failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error recording bill payment.", correlationId = CorrelationId });
            }
        }

        /// <summary>
        /// Alias PUT method for recording payment on a bill.
        /// </summary>
        [HttpPut("{id}/payment")]
        public Task<ActionResult<BillDto>> UpdatePayment(int id, [FromBody] RecordBillPaymentDto dto)
        {
            return RecordPayment(id, dto);
        }

        /// <summary>
        /// Update bill status (Pending, Paid, Cancelled, Refunded) with audit trail.
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<BillDto>> UpdateStatus(int id, [FromBody] UpdateBillStatusDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (id <= 0) return BadRequest(new { message = "Invalid bill id." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updated = await _billService.UpdateStatusAsync(
                    CurrentBusinessId, id, dto.Status, dto.PaymentMethod, dto.PaymentReference, dto.Notes);
                if (updated == null) return NotFound("Bill not found.");
                return Ok(updated);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "UpdateStatus failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Error updating bill status.", correlationId = CorrelationId });
            }
        }
    }
}
