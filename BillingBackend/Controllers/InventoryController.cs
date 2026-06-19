using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class InventoryController : BaseApiController
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryDto>>> GetAll()
        {
            var items = await _inventoryService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryDto>> GetById(int id)
        {
            var item = await _inventoryService.GetByIdAsync(CurrentBusinessId, id);
            if (item == null) return NotFound("Inventory item not found.");
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<InventoryDto>> Create(InventoryDto dto)
        {
            var created = await _inventoryService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InventoryDto>> Update(int id, InventoryDto dto)
        {
            dto.Id = id;
            var updated = await _inventoryService.UpdateAsync(CurrentBusinessId, dto);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _inventoryService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete inventory item.");
            return NoContent();
        }
    }
}
