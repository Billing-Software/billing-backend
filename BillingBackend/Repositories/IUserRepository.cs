using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<UserRegisterResultDto?> RegisterUserAndBusinessAsync(RegisterDto registerDto, byte[] passwordHash, byte[] passwordSalt);
        Task AddRefreshTokenAsync(UserRefreshToken token);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string token);
        Task RemoveRefreshTokenAsync(UserRefreshToken token);
        Task SaveChangesAsync();
    }
}
