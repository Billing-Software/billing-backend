using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class ServicesController : BaseApiController
    {
        private readonly IServiceService _serviceService;

        public ServicesController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll()
        {
            var services = await _serviceService.GetByBusinessIdAsync(CurrentBusinessId);
            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetById(int id)
        {
            var service = await _serviceService.GetByIdAsync(CurrentBusinessId, id);
            if (service == null) return NotFound("Service not found.");
            return Ok(service);
        }

        [HttpPost]
        public async Task<ActionResult<ServiceDto>> Create(ServiceDto dto)
        {
            var created = await _serviceService.AddAsync(CurrentBusinessId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ServiceDto>> Update(int id, ServiceDto dto)
        {
            dto.Id = id;
            var updated = await _serviceService.UpdateAsync(CurrentBusinessId, dto);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _serviceService.DeleteAsync(CurrentBusinessId, id);
            if (!deleted) return BadRequest("Could not delete service.");
            return NoContent();
        }
    }
}
