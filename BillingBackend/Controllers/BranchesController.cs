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
            var branches = await _branchService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(branches);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BranchDto>> GetById(int id)
        {
            var branch = await _branchService.GetByIdAsync(CurrentBusinessId, id);
            if (branch == null) return NotFound("Branch not found.");
            return Ok(branch);
        }

        [HttpPost]
        public async Task<ActionResult<BranchDto>> Create(BranchDto dto)
        {
            var created = await _branchService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BranchDto>> Update(int id, BranchDto dto)
        {
            dto.Id = id;
            var updated = await _branchService.UpdateAsync(CurrentBusinessId, dto);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _branchService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete branch.");
            return NoContent();
        }
    }
}
