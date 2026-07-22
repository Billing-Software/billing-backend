using System;
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
            try
            {
                var services = await _serviceService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(services);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching services list.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetById(int id)
        {
            try
            {
                var service = await _serviceService.GetByIdAsync(CurrentBusinessId, id);
                if (service == null) return NotFound("Service not found.");
                return Ok(service);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching service details.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ServiceDto>> Create(ServiceDto dto)
        {
            try
            {
                var created = await _serviceService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating service.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ServiceDto>> Update(int id, ServiceDto dto)
        {
            try
            {
                dto.Id = id;
                var updated = await _serviceService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating service.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _serviceService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete service.");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting service.", error = ex.Message });
            }
        }
    }
}
