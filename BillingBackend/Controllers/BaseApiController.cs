using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillingBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        protected int CurrentBusinessId
        {
            get
            {
                var raw = User.FindFirst("businessId")?.Value;
                return int.TryParse(raw, out var id) && id > 0 ? id : 0;
            }
        }

        protected int CurrentUserId
        {
            get
            {
                var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(raw, out var id) && id > 0 ? id : 0;
            }
        }

        protected string CurrentUserRole
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.Role);
                return claim != null ? claim.Value : string.Empty;
            }
        }

        protected bool IsOwnerOrSuperAdmin =>
            User.IsInRole("Owner") || User.IsInRole("SuperAdmin");

        protected string CorrelationId =>
            HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;

        /// <summary>Fail-closed guard: every tenant endpoint requires a valid business scope (except SuperAdmin).</summary>
        protected bool HasValidBusinessScope(out int businessId)
        {
            businessId = CurrentBusinessId;
            if (User.IsInRole("SuperAdmin"))
                return true;
            return businessId > 0 && CurrentUserId > 0;
        }

        protected ActionResult InvalidScope() =>
            StatusCode(403, new { message = "Invalid business scope.", correlationId = CorrelationId });
    }
}
