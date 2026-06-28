using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class PurchasesController : BaseApiController
    {
        private readonly IPurchaseService _purchaseService;

        public PurchasesController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
        {
            var purchases = await _purchaseService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(purchases);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseDto>> GetById(int id)
        {
            var purchase = await _purchaseService.GetByIdAsync(CurrentBusinessId, id);
            if (purchase == null) return NotFound("Purchase record not found.");
            return Ok(purchase);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseDto>> Create(PurchaseDto dto)
        {
            var created = await _purchaseService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _purchaseService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete purchase record.");
            return NoContent();
        }
    }
}
