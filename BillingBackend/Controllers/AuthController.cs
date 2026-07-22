using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                if (result == null)
                {
                    return BadRequest("Registration failed.");
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                if (result == null)
                {
                    return Unauthorized("Invalid email or password.");
                }
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(TokenRefreshDto refreshDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshDto);
                if (result == null)
                {
                    return Unauthorized("Invalid or expired refresh token.");
                }
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("check-username")]
        public async Task<ActionResult<bool>> CheckUsername([FromQuery] string username)
        {
            try
            {
                var exists = await _authService.UsernameExistsAsync(username);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking username availability.", error = ex.Message });
            }
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            try
            {
                var exists = await _authService.EmailExistsAsync(email);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking email availability.", error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            try
            {
                var code = await _authService.ForgotPasswordAsync(dto.Email);
                if (code == null)
                {
                    return BadRequest("Email not found.");
                }
                return Ok(new { message = "Password reset code sent successfully.", code = code });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error requesting password reset.", error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                var success = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
                if (!success)
                {
                    return BadRequest("Invalid code or code has expired.");
                }
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resetting password.", error = ex.Message });
            }
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            try
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (claim == null)
                {
                    return Unauthorized();
                }
                int userId = int.Parse(claim.Value);

                var success = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
                if (!success)
                {
                    return BadRequest("Incorrect current password.");
                }
                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing password.", error = ex.Message });
            }
        }
    }
}
