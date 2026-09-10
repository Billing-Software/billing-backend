using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class StaffController : BaseApiController
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetAll()
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var staffList = await _staffService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(staffList);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching staff members list.", correlationId = CorrelationId });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffDto>> GetById(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var staff = await _staffService.GetByIdAsync(CurrentBusinessId, id);
                if (staff == null) return NotFound("Staff member not found.");
                return Ok(staff);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching staff member details.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult<StaffDto>> Create(StaffDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _staffService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating staff member.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<StaffDto>> Update(int id, StaffDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                dto.Id = id;
                var updated = await _staffService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating staff member.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var deleted = await _staffService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete staff member.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting staff member.", correlationId = CorrelationId });
            }
        }
    }
}
