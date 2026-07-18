using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    /// <summary>
    /// WhatsApp Cloud API integration endpoints.
    /// All endpoints are JWT-protected and scoped to the authenticated business.
    /// </summary>
    public class WhatsAppController : BaseApiController
    {
        private readonly IWhatsAppService _whatsAppService;

        public WhatsAppController(IWhatsAppService whatsAppService)
        {
            _whatsAppService = whatsAppService;
        }

        /// <summary>
        /// Exchange the OAuth authorization code from Meta Embedded Signup and connect the WhatsApp account.
        /// </summary>
        [HttpPost("connect")]
        public async Task<ActionResult<WhatsAppAccountDto>> Connect(WhatsAppConnectCallbackDto dto)
        {
            try
            {
                var result = await _whatsAppService.ConnectAsync(CurrentBusinessId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get the current WhatsApp connection status.
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<WhatsAppAccountDto>> GetStatus()
        {
            var status = await _whatsAppService.GetStatusAsync(CurrentBusinessId);
            if (status == null)
            {
                return Ok(new WhatsAppAccountDto { Status = "NotConnected" });
            }
            return Ok(status);
        }

        /// <summary>
        /// Disconnect the WhatsApp account.
        /// </summary>
        [HttpDelete("disconnect")]
        public async Task<ActionResult> Disconnect()
        {
            var result = await _whatsAppService.DisconnectAsync(CurrentBusinessId);
            if (!result) return NotFound("No WhatsApp account found to disconnect.");
            return NoContent();
        }

        /// <summary>
        /// Send a text message via WhatsApp.
        /// </summary>
        [HttpPost("send-text")]
        public async Task<ActionResult<MessageLogDto>> SendText(SendTextMessageDto dto)
        {
            try
            {
                var result = await _whatsAppService.SendTextAsync(CurrentBusinessId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Send an invoice PDF document via WhatsApp.
        /// </summary>
        [HttpPost("send-document")]
        public async Task<ActionResult<MessageLogDto>> SendDocument(SendDocumentDto dto)
        {
            try
            {
                var result = await _whatsAppService.SendDocumentAsync(CurrentBusinessId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Send a template message via WhatsApp.
        /// </summary>
        [HttpPost("send-template")]
        public async Task<ActionResult<MessageLogDto>> SendTemplate(SendTemplateDto dto)
        {
            try
            {
                var result = await _whatsAppService.SendTemplateAsync(CurrentBusinessId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get message logs, optionally filtered by bill ID.
        /// </summary>
        [HttpGet("messages")]
        public async Task<ActionResult<IEnumerable<MessageLogDto>>> GetMessages([FromQuery] int? billId = null)
        {
            var logs = await _whatsAppService.GetMessageLogsAsync(CurrentBusinessId, billId);
            return Ok(logs);
        }
    }
}
