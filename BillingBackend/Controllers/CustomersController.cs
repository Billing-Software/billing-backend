using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class CustomersController : BaseApiController
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
        {
            try
            {
                var customers = await _customerService.GetByBusinessIdAsync(CurrentBusinessId);
                return Ok(customers);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching customers.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
        {
            try
            {
                var customer = await _customerService.GetByIdAsync(CurrentBusinessId, id);
                if (customer == null) return NotFound("Customer not found.");
                return Ok(customer);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching customer details.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDto>> Create(CustomerDto dto)
        {
            try
            {
                var created = await _customerService.AddAsync(CurrentBusinessId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error creating customer.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerDto>> Update(int id, CustomerDto dto)
        {
            try
            {
                dto.Id = id;
                var updated = await _customerService.UpdateAsync(CurrentBusinessId, dto);
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating customer.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _customerService.DeleteAsync(CurrentBusinessId, id);
                if (!deleted) return BadRequest("Could not delete customer.");
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting customer.", error = ex.Message });
            }
        }
    }
}
