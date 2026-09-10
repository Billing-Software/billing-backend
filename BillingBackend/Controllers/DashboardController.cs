using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDataDto>> GetDashboardData()
        {
            try
            {
                if (CurrentUserRole != "Owner")
                {
                    var data = await _dashboardService.GetStaffDashboardDataAsync(CurrentBusinessId, CurrentUserId);
                    return Ok(data);
                }
                else
                {
                    var data = await _dashboardService.GetDashboardDataAsync(CurrentBusinessId);
                    return Ok(data);
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error loading dashboard metrics.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
