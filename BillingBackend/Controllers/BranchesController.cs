using BillingBackend.DTOs;
using BillingBackend.Services;
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
            try
            {
                var branches = await _branchService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(branches);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching branches.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BranchDto>> GetById(int id)
        {
            try
            {
                var branch = await _branchService.GetByIdAsync(CurrentBusinessId, id);
                if (branch == null) return NotFound("Branch not found.");
                return Ok(branch);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching branch details.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BranchDto>> Create(BranchDto dto)
        {
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
                return StatusCode(500, new { message = "Error creating branch.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BranchDto>> Update(int id, BranchDto dto)
        {
            try
            {
                dto.Id = id;
                var updated = await _branchService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating branch.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _branchService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete branch.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting branch.", error = ex.Message });
            }
        }
    }
}
