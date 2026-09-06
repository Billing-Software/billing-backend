using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;
using BillingBackend.Services.Sms;
using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    /// <summary>
    /// Multi-tenant SMS Controller (Exotel Engine with DLT Compliance).
    /// All endpoints are authenticated and isolated to CurrentBusinessId.
    /// Store owners configure their DLT Sender ID & Template IDs here.
    /// Exotel API credentials stay only on the BillCom backend.
    /// </summary>
    public class SmsController : BaseApiController
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        /// <summary>
        /// Get SMS configuration for the authenticated business.
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<BusinessSmsSettingsDto>> GetSettings()
        {
            try
            {
                var settings = await _smsService.GetSettingsAsync(CurrentBusinessId);
                if (settings == null)
                {
                    return Ok(new BusinessSmsSettingsDto
                    {
                        BusinessId = CurrentBusinessId,
                        Provider = "Exotel",
                        IsActive = false
                    });
                }
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve SMS settings.", error = ex.Message });
            }
        }

        /// <summary>
        /// Save or update DLT SMS configuration for the authenticated business.
        /// </summary>
        [HttpPost("settings")]
        public async Task<ActionResult<BusinessSmsSettingsDto>> SaveSettings([FromBody] UpdateSmsSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _smsService.SaveSettingsAsync(CurrentBusinessId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to save SMS settings.", error = ex.Message });
            }
        }

        /// <summary>
        /// Send a test SMS to verify the configured DLT Sender ID and delivery.
        /// </summary>
        [HttpPost("send-test")]
        public async Task<ActionResult<SmsResult>> SendTestSms([FromBody] SendTestSmsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var message = !string.IsNullOrWhiteSpace(dto.Message)
                    ? dto.Message
                    : "Test SMS from BillCom. Your Exotel DLT Sender ID is configured and working!";

                var result = await _smsService.SendAsync(CurrentBusinessId, dto.Phone, message);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Error });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to dispatch test SMS.", error = ex.Message });
            }
        }

        /// <summary>
        /// Send an invoice SMS to customer.
        /// </summary>
        [HttpPost("send-invoice")]
        public async Task<ActionResult<SmsResult>> SendInvoiceSms([FromBody] SendInvoiceSmsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _smsService.SendInvoiceSmsAsync(CurrentBusinessId, dto.BillId, dto.Phone);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Error });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to dispatch invoice SMS.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get SMS dispatch logs for the authenticated business.
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<IEnumerable<SmsLogDto>>> GetLogs([FromQuery] int? billId = null)
        {
            try
            {
                var logs = await _smsService.GetLogsAsync(CurrentBusinessId, billId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve SMS logs.", error = ex.Message });
            }
        }
    }
}
