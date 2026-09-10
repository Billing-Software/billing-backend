using System;
using System.Threading.Tasks;
using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    /// <summary>
    /// Business settings controller managing decoupled, relational database tables:
    /// Payment/Bank, Statutory Tax/GST, Invoice Numbering & Sequence, Print Layout & Visual Themes, Thermal Printers, and Preferences.
    /// </summary>
    public class SettingsController : BaseApiController
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        #region Payment & Banking (Domain 5)

        [HttpGet("payment")]
        public async Task<ActionResult<PaymentSettingsDto>> GetPaymentSettings()
        {
            try
            {
                var settings = await _settingsService.GetPaymentSettingsAsync(CurrentBusinessId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving payment settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("payment")]
        public async Task<ActionResult<PaymentSettingsDto>> SavePaymentSettings([FromBody] PaymentSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SavePaymentSettingsAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving payment settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("payment")]
        public Task<ActionResult<PaymentSettingsDto>> UpdatePaymentSettings([FromBody] PaymentSettingsDto dto)
        {
            return SavePaymentSettings(dto);
        }

        #endregion

        #region Tax & GST Compliance (Domain 4)

        [HttpGet("tax")]
        public async Task<ActionResult<TaxSettingsDto>> GetTaxSettings()
        {
            try
            {
                var settings = await _settingsService.GetTaxSettingsAsync(CurrentBusinessId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tax settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("tax")]
        public async Task<ActionResult<TaxSettingsDto>> SaveTaxSettings([FromBody] TaxSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SaveTaxSettingsAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving tax settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("tax")]
        public Task<ActionResult<TaxSettingsDto>> UpdateTaxSettings([FromBody] TaxSettingsDto dto)
        {
            return SaveTaxSettings(dto);
        }

        #endregion

        #region Invoice Numbering & Sequence Rules (Domain 3)

        [HttpGet("invoice")]
        public async Task<ActionResult<InvoiceSettingsDto>> GetInvoiceSettings()
        {
            try
            {
                var settings = await _settingsService.GetInvoiceSettingsAsync(CurrentBusinessId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving invoice settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("invoice")]
        public async Task<ActionResult<InvoiceSettingsDto>> SaveInvoiceSettings([FromBody] InvoiceSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SaveInvoiceSettingsAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving invoice settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("invoice")]
        public Task<ActionResult<InvoiceSettingsDto>> UpdateInvoiceSettings([FromBody] InvoiceSettingsDto dto)
        {
            return SaveInvoiceSettings(dto);
        }

        #endregion

        #region Invoice Visual Appearance & Print Themes (Domain 6)

        [HttpGet("invoice-design")]
        public async Task<ActionResult<InvoiceDesignDto>> GetInvoiceDesign()
        {
            try
            {
                var settings = await _settingsService.GetInvoiceDesignAsync(CurrentBusinessId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving invoice design.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("invoice-design")]
        public async Task<ActionResult<InvoiceDesignDto>> SaveInvoiceDesign([FromBody] InvoiceDesignDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SaveInvoiceDesignAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving invoice design.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("invoice-design")]
        public Task<ActionResult<InvoiceDesignDto>> UpdateInvoiceDesign([FromBody] InvoiceDesignDto dto)
        {
            return SaveInvoiceDesign(dto);
        }

        #endregion

        #region Hardware: Thermal POS Printers

        [HttpGet("printer")]
        public async Task<ActionResult<PrinterSettingsDto>> GetPrinterSettings([FromQuery] int? branchId)
        {
            try
            {
                var settings = await _settingsService.GetPrinterSettingsAsync(CurrentBusinessId, branchId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving printer settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("printer")]
        public async Task<ActionResult<PrinterSettingsDto>> SavePrinterSettings([FromBody] PrinterSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SavePrinterSettingsAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving printer settings.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("printer")]
        public Task<ActionResult<PrinterSettingsDto>> UpdatePrinterSettings([FromBody] PrinterSettingsDto dto)
        {
            return SavePrinterSettings(dto);
        }

        #endregion

        #region Regional Preferences & UI Formatting

        [HttpGet("preferences")]
        public async Task<ActionResult<AppPreferencesDto>> GetAppPreferences()
        {
            try
            {
                var settings = await _settingsService.GetAppPreferencesAsync(CurrentBusinessId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving app preferences.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPost("preferences")]
        public async Task<ActionResult<AppPreferencesDto>> SaveAppPreferences([FromBody] AppPreferencesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var saved = await _settingsService.SaveAppPreferencesAsync(CurrentBusinessId, dto);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving app preferences.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        [Authorize(Roles = "Owner,SuperAdmin")]
        [HttpPut("preferences")]
        public Task<ActionResult<AppPreferencesDto>> UpdateAppPreferences([FromBody] AppPreferencesDto dto)
        {
            return SaveAppPreferences(dto);
        }

        #endregion
    }
}
