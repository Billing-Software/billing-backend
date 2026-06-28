using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                var claim = User.FindFirst("businessId");
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        protected int CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        protected string CurrentUserRole
        {
            get
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
                return claim != null ? claim.Value : string.Empty;
            }
        }
    }
}
