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

        public AuthService(IUserRepository userRepository, IBusinessRepository businessRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _tokenService = tokenService;
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

            return new AuthResponseDto
            {
                Token = token,
                Username = result.Username,
                Email = result.Email,
                Role = result.Role,
                BusinessId = result.BusinessId,
                BusinessName = result.BusinessName
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsernameAsync(loginDto.Username);
            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(loginDto.Username);
            }

            if (user == null)
            {
                Console.WriteLine($"[AUTH_DEBUG] Login failed: User identity '{loginDto.Username}' not found in database by username or email.");
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

            // Fetch the business details for this owner
            var business = await _businessRepository.GetByOwnerIdAsync(user.Id);
            int businessId = business?.Id ?? 0;
            string businessName = business?.LegalName ?? string.Empty;

            var token = _tokenService.CreateToken(user, businessId);

            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                BusinessId = businessId,
                BusinessName = businessName
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
    }
}
