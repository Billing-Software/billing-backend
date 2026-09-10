using System;
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
            try
            {
                var items = await _inventoryService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching inventory items.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryDto>> GetById(int id)
        {
            try
            {
                var item = await _inventoryService.GetByIdAsync(CurrentBusinessId, id);
                if (item == null) return NotFound("Inventory item not found.");
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching inventory details.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        public async Task<ActionResult<InventoryDto>> Create(InventoryDto dto)
        {
            try
            {
                var created = await _inventoryService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating inventory item.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InventoryDto>> Update(int id, InventoryDto dto)
        {
            try
            {
                dto.Id = id;
                var updated = await _inventoryService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating inventory item.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _inventoryService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete inventory item.");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting inventory item.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
