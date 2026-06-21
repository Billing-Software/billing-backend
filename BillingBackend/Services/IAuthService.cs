using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
    }
}
