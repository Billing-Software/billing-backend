using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class BusinessController : BaseApiController
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        [HttpGet]
        public async Task<ActionResult<BusinessDto>> GetProfile()
        {
            try
            {
                var business = await _businessService.GetByIdAsync(CurrentBusinessId);
                if (business == null) return NotFound("Business not found.");
                return Ok(business);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving business profile.", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<BusinessDto>> UpdateProfile(BusinessDto dto)
        {
            try
            {
                if (dto.Id != CurrentBusinessId)
                {
                    return BadRequest("Invalid business ID.");
                }

                var updated = await _businessService.UpdateAsync(dto);
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error updating business profile.", error = ex.Message });
            }
        }
    }
}
