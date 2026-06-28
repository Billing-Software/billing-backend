using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BillingDbContext _context;

        public UserRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<UserRegisterResultDto?> RegisterUserAndBusinessAsync(RegisterDto registerDto, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Insert User
                    var user = new User
                    {
                        Username = registerDto.Username,
                        Email = registerDto.Email,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = string.IsNullOrEmpty(registerDto.Role) ? "Owner" : registerDto.Role
                    };
                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();

                    // 2. Insert Business
                    var business = new Business
                    {
                        OwnerId = user.Id,
                        LegalName = registerDto.LegalName,
                        TradingName = registerDto.TradingName,
                        LogoUrl = registerDto.LogoUrl,
                        Address = registerDto.BusinessAddress,
                        City = registerDto.BusinessCity,
                        State = registerDto.BusinessState,
                        PostalCode = registerDto.BusinessPostalCode,
                        Country = registerDto.BusinessCountry ?? "India",
                        Phone = registerDto.BusinessPhone,
                        Email = registerDto.BusinessEmail ?? registerDto.Email,
                        Website = registerDto.Website,
                        GstIn = registerDto.GstIn,
                        DefaultTaxRate = registerDto.DefaultTaxRate,
                        PricesIncludeTax = registerDto.PricesIncludeTax
                    };
                    await _context.Businesses.AddAsync(business);
                    await _context.SaveChangesAsync();

                    // 3. Insert Default Branch
                    var branch = new Branch
                    {
                        BusinessId = business.Id,
                        Name = "Main Branch",
                        Address = registerDto.BusinessAddress,
                        City = registerDto.BusinessCity,
                        PostalCode = registerDto.BusinessPostalCode,
                        Phone = registerDto.BusinessPhone,
                        IsActive = true
                    };
                    await _context.Branches.AddAsync(branch);
                    await _context.SaveChangesAsync();

                    // 4. Insert Default Walk-In Customer
                    var customer = new Customer
                    {
                        BusinessId = business.Id,
                        Name = "Walk-In Customer",
                        Phone = "N/A",
                        Email = null,
                        IsWalkIn = true
                    };
                    await _context.Customers.AddAsync(customer);

                    // 5. Insert WhatsApp Settings
                    var waSettings = new WhatsAppSettings
                    {
                        BusinessId = business.Id,
                        ApiKey = null,
                        IsConnected = false
                    };
                    await _context.WhatsAppSettings.AddAsync(waSettings);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new UserRegisterResultDto
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        BusinessId = business.Id,
                        BusinessName = business.LegalName,
                        DefaultBranchId = branch.Id
                    };
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    throw new Exception($"Database update failed: {inner}", dbEx);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
