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
    }
}
