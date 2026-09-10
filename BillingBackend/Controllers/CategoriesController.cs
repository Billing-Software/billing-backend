using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            try
            {
                var categories = await _categoryService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(categories);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching categories.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create(CategoryDto dto)
        {
            try
            {
                var created = await _categoryService.AddAsync(CurrentBusinessId, dto);
                return Ok(created);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, CategoryDto dto)
        {
            try
            {
                var updated = await _categoryService.UpdateAsync(CurrentBusinessId, id, dto);
                if (updated == null)
                {
                    return NotFound();
                }
                return Ok(updated);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _categoryService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete category.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
