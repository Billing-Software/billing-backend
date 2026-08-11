using System;
using System.Threading.Tasks;
using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    [Authorize]
    public class ReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Get detailed monthly GST tax report, slabs breakdown, HSN summaries, and daily trends.
        /// </summary>
        [HttpGet("gst-summary")]
        public async Task<ActionResult<GstReportSummaryDto>> GetGstReport(
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? branchId)
        {
            try
            {
                var report = await _reportService.GetGstReportAsync(CurrentBusinessId, month, year, startDate, endDate, branchId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating GST report summary.", error = ex.Message });
            }
        }

        /// <summary>
        /// Download GSTR tax summary report as CSV file for accountants or Excel import.
        /// </summary>
        [HttpGet("gst-summary/export-csv")]
        public async Task<IActionResult> ExportGstReportCsv(
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? branchId)
        {
            try
            {
                var csvBytes = await _reportService.ExportGstReportCsvAsync(CurrentBusinessId, month, year, startDate, endDate, branchId);
                string fileName = $"GST_Tax_Report_{DateTime.UtcNow:yyyy_MM_dd}.csv";
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting GST report CSV.", error = ex.Message });
            }
        }
    }
}
