using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class BranchesController : BaseApiController
    {
        private readonly IBranchService _branchService;

        public BranchesController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BranchDto>>> GetAll()
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var branches = await _branchService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(branches);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching branches.", correlationId = CorrelationId });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BranchDto>> GetById(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var branch = await _branchService.GetByIdAsync(CurrentBusinessId, id);
                if (branch == null) return NotFound("Branch not found.");
                return Ok(branch);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching branch details.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult<BranchDto>> Create(BranchDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _branchService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating branch.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<BranchDto>> Update(int id, BranchDto dto)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                dto.Id = id;
                var updated = await _branchService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating branch.", correlationId = CorrelationId });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!HasValidBusinessScope(out _)) return InvalidScope();
            try
            {
                var deleted = await _branchService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete branch.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting branch.", correlationId = CorrelationId });
            }
        }
    }
}
