using BillingBackend.Data.Entities;
using BillingBackend.Repositories;
using BillingBackend.DTOs;
using System;
using System.Security.Cryptography;
using System.Text;
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

        public AuthService(IUserRepository userRepository, IBusinessRepository businessRepository, ITokenService tokenService, IStaffRepository staffRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _tokenService = tokenService;
            _staffRepository = staffRepository;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username or email already exists
            if (await _userRepository.GetByUsernameAsync(registerDto.Username) != null)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            if (await _userRepository.GetByEmailAsync(registerDto.Email) != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            using var hmac = new HMACSHA512();
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            var passwordSalt = hmac.Key;

            // Use transactional registration (User + Business + Default Branch)
            var result = await _userRepository.RegisterUserAndBusinessAsync(registerDto, passwordHash, passwordSalt);
            if (result == null)
            {
                throw new InvalidOperationException("Registration failed.");
            }

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
            bool onboardingPending = false;
            if (business != null)
            {
                onboardingPending = string.IsNullOrEmpty(business.Address) || 
                                    string.IsNullOrEmpty(business.City) || 
                                    string.IsNullOrEmpty(business.Phone);
            }

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
                var pending = await _userRepository.GetPendingRegistrationByEmailAsync(loginDto.Email);
                if (pending != null)
                {
                    throw new InvalidOperationException("Account activation pending: Please complete your subscription payment on our web portal to activate your account.");
                }

                Console.WriteLine($"[AUTH_DEBUG] Login failed: User identity '{loginDto.Email}' not found in database by email.");
                return null;
            }

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                {
                    Console.WriteLine($"[AUTH_DEBUG] Login failed: Password hash mismatch for user '{user.Username}'.");
                    return null;
                }
            }

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
                {
                    throw new InvalidOperationException($"You do not have access to Business ID {loginDto.BusinessId.Value}.");
                }
                
                if (staff.Status == "Inactive")
                {
                    throw new InvalidOperationException("Access denied: Staff account is suspended.");
                }

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
                {
                    throw new InvalidOperationException("Your business account has been suspended. Please contact platform support.");
                }
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
            if (user == null)
            {
                return null;
            }

            // Generate a 6-digit code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            user.PasswordResetToken = code;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.SaveChangesAsync();

            // Prepare email template
            var subject = "BillCom - Password Reset Code";
            var body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                    <h2 style='color: #006a61; text-align: center; font-family: Outfit, sans-serif;'>BillCom</h2>
                    <p>Hello,</p>
                    <p>We received a request to reset your password. Use the verification code below to complete the reset process:</p>
                    <div style='background-color: #f8f9ff; border: 1px dashed #006a61; padding: 15px; text-align: center; font-size: 26px; font-weight: bold; letter-spacing: 4px; color: #0b1c30; border-radius: 6px; margin: 20px 0;'>
                        {code}
                    </div>
                    <p style='font-size: 11px; color: #7c839b;'>This code is valid for 15 minutes. If you did not request this, you can safely ignore this email.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"[EMAIL SERVICE ERROR] Failed to send email via SMTP: {ex.Message}");
                Console.WriteLine($"[EMAIL SERVICE FALLBACK] Reset Code: {code}");
                Console.WriteLine($"========================================\n");
            }

            return code;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return false;
            }

            using var hmac = new HMACSHA512();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
            user.PasswordSalt = hmac.Key;

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(currentPassword));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                {
                    return false;
                }
            }

            using var newHmac = new HMACSHA512();
            user.PasswordHash = newHmac.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
            user.PasswordSalt = newHmac.Key;

            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(TokenRefreshDto refreshDto)
        {
            var dbRefreshToken = await _userRepository.GetRefreshTokenAsync(refreshDto.RefreshToken);
            if (dbRefreshToken == null || dbRefreshToken.IsRevoked || dbRefreshToken.ExpiryTime < DateTime.UtcNow)
            {
                return null;
            }

            var user = dbRefreshToken.User;
            if (user == null)
            {
                return null;
            }

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
                {
                    return null;
                }
                
                if (staff.Status == "Inactive")
                {
                    throw new InvalidOperationException("Access denied: Staff account is suspended.");
                }

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
                {
                    throw new InvalidOperationException("Your business account has been suspended. Please contact platform support.");
                }
            }

            // Generate new access token
            var token = _tokenService.CreateToken(user, businessId, staffId);

            // Rotate refresh token: delete the old one
            await _userRepository.RemoveRefreshTokenAsync(dbRefreshToken);

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
    }
}
