using BillingBackend.Data.Entities;
using BillingBackend.Repositories;
using BillingBackend.DTOs;
using BillingBackend.Security;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly ITokenService _tokenService;
        private readonly IStaffRepository _staffRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        private const int MaxFailedLogins = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private const int MaxOtpAttempts = 5;
        private const int MaxOtpRequestsPerWindow = 3;
        private static readonly TimeSpan OtpRequestWindow = TimeSpan.FromMinutes(15);

        public AuthService(
            IUserRepository userRepository,
            IBusinessRepository businessRepository,
            ITokenService tokenService,
            IStaffRepository staffRepository,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _tokenService = tokenService;
            _staffRepository = staffRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Server-authoritative role/plan: never trust client Role/PlanId for self-service.
            registerDto.Role = "Owner";
            registerDto.PlanId = 1;

            var (pwOk, pwError) = PasswordPolicy.Validate(registerDto.Password);
            if (!pwOk)
                throw new InvalidOperationException(pwError ?? "Password does not meet policy.");

            // Generic existence check (avoid distinguishing which field exists in error detail where possible).
            if (await _userRepository.GetByUsernameAsync(registerDto.Username) != null)
                throw new InvalidOperationException("Username already exists.");

            if (await _userRepository.GetByEmailAsync(registerDto.Email) != null)
                throw new InvalidOperationException("Email already exists.");

            PasswordHasher.CreateHash(registerDto.Password, out var passwordHash, out var passwordSalt);

            // Use transactional registration (User + Business + Default Branch)
            var result = await _userRepository.RegisterUserAndBusinessAsync(registerDto, passwordHash, passwordSalt);
            if (result == null)
                throw new InvalidOperationException("Registration failed.");

            var dummyUser = new User
            {
                Id = result.UserId,
                Username = result.Username,
                Email = result.Email,
                Role = result.Role
            };

            var token = _tokenService.CreateToken(dummyUser, result.BusinessId);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var dbRefreshToken = new UserRefreshToken
            {
                UserId = dummyUser.Id,
                Token = refreshToken,
                ExpiryTime = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(dbRefreshToken);
            await _userRepository.SaveChangesAsync();

            var business = await _businessRepository.GetByIdAsync(result.BusinessId);
            bool onboardingPending = business != null &&
                (string.IsNullOrEmpty(business.Address) ||
                 string.IsNullOrEmpty(business.City) ||
                 string.IsNullOrEmpty(business.Phone));

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Username = result.Username,
                Email = result.Email,
                Role = result.Role,
                BusinessId = result.BusinessId,
                BusinessName = result.BusinessName,
                OnboardingPending = onboardingPending
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                // Generic failure — do not reveal whether email exists or activation is pending.
                _logger.LogWarning("Login failed: unknown email.");
                return null;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                throw new InvalidOperationException($"Account temporarily locked. Try again after {user.LockoutEnd.Value:u}.");

            if (!PasswordHasher.Verify(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedLogins)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                    _logger.LogWarning("Account locked due to repeated failures. UserId={UserId}", user.Id);
                }
                await _userRepository.SaveChangesAsync();
                return null;
            }

            // Transparent upgrade of legacy HMAC hashes to PBKDF2.
            if (PasswordHasher.IsLegacyHash(user.PasswordHash, user.PasswordSalt))
            {
                PasswordHasher.CreateHash(loginDto.Password, out var nh, out var ns);
                user.PasswordHash = nh;
                user.PasswordSalt = ns;
            }
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepository.SaveChangesAsync();

            // Fetch the business details for this owner or staff member
            int businessId = 0;
            string businessName = string.Empty;
            int? staffId = null;

            if (loginDto.BusinessId.HasValue)
            {
                // Convert 4-digit ID back to DB ID (e.g. 1001 -> 1)
                int dbBusinessId = loginDto.BusinessId.Value - 1000;
                var staff = await _staffRepository.GetByUserIdAndBusinessIdAsync(user.Id, dbBusinessId);
                if (staff == null)
                    throw new InvalidOperationException($"You do not have access to Business ID {loginDto.BusinessId.Value}.");

                if (staff.Status == "Inactive")
                    throw new InvalidOperationException("Access denied: Staff account is suspended.");

                businessId = staff.BusinessId;
                staffId = staff.Id;
                var business = await _businessRepository.GetByIdAsync(businessId);
                businessName = business?.LegalName ?? string.Empty;
            }
            else
            {
                if (user.Role == "Owner")
                {
                    var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                    businessId = business?.Id ?? 0;
                    businessName = business?.LegalName ?? string.Empty;
                }
                else if (user.Role == "SuperAdmin")
                {
                    var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                    businessId = business?.Id ?? 0;
                    businessName = business?.LegalName ?? "System Administration";
                }
                else
                {
                    throw new InvalidOperationException("Business ID is required for staff login.");
                }
            }

            // Check if client business is suspended (bypassed for SuperAdmin)
            if (user.Role != "SuperAdmin" && businessId > 0)
            {
                var business = await _businessRepository.GetByIdAsync(businessId);
                if (business != null && business.IsSuspended)
                    throw new InvalidOperationException("Your business account has been suspended. Please contact platform support.");
            }

            var token = _tokenService.CreateToken(user, businessId, staffId);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var dbRefreshToken = new UserRefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryTime = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(dbRefreshToken);
            await _userRepository.SaveChangesAsync();

            bool onboardingPending = false;
            if (user.Role == "Owner")
            {
                var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                if (business != null)
                {
                    onboardingPending = string.IsNullOrEmpty(business.Address) ||
                                        string.IsNullOrEmpty(business.City) ||
                                        string.IsNullOrEmpty(business.Phone);
                }
            }

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                BusinessId = businessId + 1000, // Return as 4-digit ID (e.g. 1 -> 1001)
                BusinessName = businessName,
                StaffId = staffId,
                OnboardingPending = onboardingPending
            };
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username) != null;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email) != null;
        }

        public async Task<string?> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            // Always behave identically to prevent enumeration; only send mail when user exists.
            if (user == null)
                return "sent";

            var now = DateTime.UtcNow;
            if (user.PasswordResetRequestedAt.HasValue &&
                now - user.PasswordResetRequestedAt.Value < OtpRequestWindow &&
                user.PasswordResetAttemptCount >= MaxOtpRequestsPerWindow)
            {
                // Throttle silently — still return success to avoid oracle.
                _logger.LogWarning("Password reset throttled. UserId={UserId}", user.Id);
                return "sent";
            }

            if (!user.PasswordResetRequestedAt.HasValue ||
                now - user.PasswordResetRequestedAt.Value >= OtpRequestWindow)
            {
                user.PasswordResetAttemptCount = 0;
            }

            var code = OtpHelper.GenerateNumericCode(6);
            user.PasswordResetToken = OtpHelper.Hash(code);
            user.PasswordResetTokenExpiry = now.AddMinutes(10);
            user.PasswordResetRequestedAt = now;
            user.PasswordResetAttemptCount++;
            await _userRepository.SaveChangesAsync();

            var subject = "BillCom - Password Reset Code";
            var body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                    <h2 style='color: #006a61; text-align: center; font-family: Outfit, sans-serif;'>BillCom</h2>
                    <p>Hello,</p>
                    <p>We received a request to reset your password. Use the verification code below:</p>
                    <div style='background-color: #f8f9ff; border: 1px dashed #006a61; padding: 15px; text-align: center; font-size: 26px; font-weight: bold; letter-spacing: 4px; color: #0b1c30; border-radius: 6px; margin: 20px 0;'>
                        {code}
                    </div>
                    <p style='font-size: 11px; color: #7c839b;'>This code is valid for 10 minutes. If you did not request this, you can safely ignore this email.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                // Never log or return the code. Email failures are server-side only.
                _logger.LogError(ex, "Failed to send password reset email. UserId={UserId}", user.Id);
            }

            // Never return the code to the caller.
            return "sent";
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var (pwOk, _) = PasswordPolicy.Validate(newPassword);
            if (!pwOk)
                return false;

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordResetToken) ||
                user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return false;

            if (!OtpHelper.Verify(token, user.PasswordResetToken))
                return false;

            PasswordHasher.CreateHash(newPassword, out var nh, out var ns);
            user.PasswordHash = nh;
            user.PasswordSalt = ns;

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.PasswordResetAttemptCount = 0;
            user.PasswordResetRequestedAt = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepository.SaveChangesAsync();

            // Revoke all sessions on credential change (industrial session hygiene).
            await _userRepository.RevokeAllRefreshTokensAsync(user.Id);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var (pwOk, _) = PasswordPolicy.Validate(newPassword);
            if (!pwOk)
                return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash, user.PasswordSalt))
                return false;

            PasswordHasher.CreateHash(newPassword, out var nh, out var ns);
            user.PasswordHash = nh;
            user.PasswordSalt = ns;
            await _userRepository.SaveChangesAsync();

            await _userRepository.RevokeAllRefreshTokensAsync(user.Id);
            return true;
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(TokenRefreshDto refreshDto)
        {
            if (string.IsNullOrWhiteSpace(refreshDto.RefreshToken))
                return null;

            var dbRefreshToken = await _userRepository.GetRefreshTokenAsync(refreshDto.RefreshToken);
            if (dbRefreshToken == null)
                return null;

            // Reuse / theft detection: revoked or expired token presented => revoke entire family.
            if (dbRefreshToken.IsRevoked || dbRefreshToken.ExpiryTime < DateTime.UtcNow)
            {
                try { await _userRepository.RevokeAllRefreshTokensAsync(dbRefreshToken.UserId); } catch { }
                _logger.LogWarning("Refresh token reuse detected. UserId={UserId}", dbRefreshToken.UserId);
                return null;
            }

            var user = dbRefreshToken.User;
            if (user == null)
                return null;

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                throw new InvalidOperationException("Account temporarily locked.");

            // Fetch the business details for this owner or staff member
            int businessId = 0;
            string businessName = string.Empty;
            int? staffId = null;

            if (user.Role == "Owner")
            {
                var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                businessId = business?.Id ?? 0;
                businessName = business?.LegalName ?? string.Empty;
            }
            else if (user.Role == "SuperAdmin")
            {
                var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                businessId = business?.Id ?? 0;
                businessName = business?.LegalName ?? "System Administration";
            }
            else
            {
                var staff = await _staffRepository.GetByUserIdAsync(user.Id);
                if (staff == null)
                    return null;

                if (staff.Status == "Inactive")
                    throw new InvalidOperationException("Access denied: Staff account is suspended.");

                businessId = staff.BusinessId;
                staffId = staff.Id;
                var business = await _businessRepository.GetByIdAsync(businessId);
                businessName = business?.LegalName ?? string.Empty;
            }

            // Check if client business is suspended (bypassed for SuperAdmin)
            if (user.Role != "SuperAdmin" && businessId > 0)
            {
                var business = await _businessRepository.GetByIdAsync(businessId);
                if (business != null && business.IsSuspended)
                    throw new InvalidOperationException("Your business account has been suspended. Please contact platform support.");
            }

            // Generate new access token
            var token = _tokenService.CreateToken(user, businessId, staffId);

            // Rotate refresh token: revoke (not just delete) the old one for reuse detection.
            dbRefreshToken.IsRevoked = true;
            await _userRepository.SaveChangesAsync();

            // Generate new refresh token
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            var newDbRefreshToken = new UserRefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                ExpiryTime = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(newDbRefreshToken);
            await _userRepository.SaveChangesAsync();

            bool onboardingPending = false;
            if (user.Role == "Owner")
            {
                var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
                if (business != null)
                {
                    onboardingPending = string.IsNullOrEmpty(business.Address) ||
                                        string.IsNullOrEmpty(business.City) ||
                                        string.IsNullOrEmpty(business.Phone);
                }
            }

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = newRefreshTokenString,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                BusinessId = user.Role == "SuperAdmin" ? businessId : businessId + 1000,
                BusinessName = businessName,
                StaffId = staffId,
                OnboardingPending = onboardingPending
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;
            var stored = await _userRepository.GetRefreshTokenAsync(refreshToken);
            if (stored == null)
                return true;
            stored.IsRevoked = true;
            await _userRepository.SaveChangesAsync();
            return true;
        }
    }
}
