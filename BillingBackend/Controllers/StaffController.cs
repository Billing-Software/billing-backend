using BillingBackend.DTOs;
using BillingBackend.Services;
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
            var staffList = await _staffService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(staffList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffDto>> GetById(int id)
        {
            var staff = await _staffService.GetByIdAsync(CurrentBusinessId, id);
            if (staff == null) return NotFound("Staff member not found.");
            return Ok(staff);
        }

        [HttpPost]
        public async Task<ActionResult<StaffDto>> Create(StaffDto dto)
        {
            var created = await _staffService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StaffDto>> Update(int id, StaffDto dto)
        {
            dto.Id = id;
            var updated = await _staffService.UpdateAsync(CurrentBusinessId, dto);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _staffService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete staff member.");
            return NoContent();
        }
    }
}
