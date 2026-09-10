using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        private string CorrelationId =>
            HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;

        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                if (result == null)
                    return BadRequest(new { message = "Registration failed.", correlationId = CorrelationId });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, correlationId = CorrelationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Registration failed. Please try again.", correlationId = CorrelationId });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                if (result == null)
                    return Unauthorized(new { message = "Invalid email or password.", correlationId = CorrelationId });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, correlationId = CorrelationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Login failed. Please try again.", correlationId = CorrelationId });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(TokenRefreshDto refreshDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshDto);
                if (result == null)
                    return Unauthorized(new { message = "Invalid or expired refresh token.", correlationId = CorrelationId });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, correlationId = CorrelationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Could not refresh session.", correlationId = CorrelationId });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(TokenRefreshDto refreshDto)
        {
            try
            {
                await _authService.LogoutAsync(refreshDto.RefreshToken);
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Logout failed.", correlationId = CorrelationId });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpGet("check-username")]
        public async Task<ActionResult<bool>> CheckUsername([FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || username.Length > 100)
                    return BadRequest(new { message = "Invalid username.", correlationId = CorrelationId });
                var exists = await _authService.UsernameExistsAsync(username.Trim());
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckUsername failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Could not check username.", correlationId = CorrelationId });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
                    return BadRequest(new { message = "Invalid email.", correlationId = CorrelationId });
                var exists = await _authService.EmailExistsAsync(email.Trim());
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckEmail failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Could not check email.", correlationId = CorrelationId });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                // Always return success to prevent account enumeration. Never return the code.
                await _authService.ForgotPasswordAsync(dto.Email.Trim());
                return Ok(new { message = "If an account exists for this email, a reset code has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPassword failed. CorrelationId={CorrelationId}", CorrelationId);
                return Ok(new { message = "If an account exists for this email, a reset code has been sent." });
            }
        }

        [EnableRateLimiting("strict")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var success = await _authService.ResetPasswordAsync(dto.Email.Trim(), dto.Token.Trim(), dto.NewPassword);
                if (!success)
                    return BadRequest(new { message = "Invalid code or code has expired.", correlationId = CorrelationId });
                return Ok(new { message = "Password reset successfully. All sessions have been revoked, please login again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Could not reset password.", correlationId = CorrelationId });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (claim == null || !int.TryParse(claim.Value, out var userId) || userId <= 0)
                    return Unauthorized(new { message = "Invalid session.", correlationId = CorrelationId });

                var success = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
                if (!success)
                    return BadRequest(new { message = "Incorrect current password or new password does not meet policy.", correlationId = CorrelationId });
                return Ok(new { message = "Password updated successfully. All other sessions revoked." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangePassword failed. CorrelationId={CorrelationId}", CorrelationId);
                return StatusCode(500, new { message = "Could not change password.", correlationId = CorrelationId });
            }
        }
    }
}
