using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    public class TaxController : BaseApiController
    {
        private readonly ITaxService _taxService;

        public TaxController(ITaxService taxService)
        {
            _taxService = taxService;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<TaxCategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _taxService.GetTaxCategoriesAsync(CurrentBusinessId);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching tax categories.", error = ex.Message });
            }
        }

        [HttpPost("categories")]
        public async Task<ActionResult<TaxCategoryDto>> CreateCategory([FromBody] TaxCategoryDto dto)
        {
            try
            {
                var created = await _taxService.CreateTaxCategoryAsync(CurrentBusinessId, dto);
                return Ok(created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating tax category.", error = ex.Message });
            }
        }

        [HttpGet("hsn/search")]
        public async Task<ActionResult<IEnumerable<HSNMaster>>> SearchHSN([FromQuery] string query = "")
        {
            try
            {
                var results = await _taxService.SearchHSNAsync(query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching HSN codes.", error = ex.Message });
            }
        }

        [HttpGet("sac/search")]
        public async Task<ActionResult<IEnumerable<SACMaster>>> SearchSAC([FromQuery] string query = "")
        {
            try
            {
                var results = await _taxService.SearchSACAsync(query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching SAC codes.", error = ex.Message });
            }
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<TaxCalculationResultDto>> CalculateTax([FromBody] TaxCalculationRequestDto request)
        {
            try
            {
                var result = await _taxService.CalculateTaxAsync(CurrentBusinessId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating tax.", error = ex.Message });
            }
        }
    }
}
