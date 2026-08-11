using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
                        BusinessType = string.IsNullOrEmpty(registerDto.BusinessType) ? "General Retail Store" : registerDto.BusinessType,
                        GstScheme = string.IsNullOrEmpty(registerDto.GstScheme) ? "Regular" : registerDto.GstScheme,
                        RegisteredState = registerDto.RegisteredState ?? registerDto.BusinessState,
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

                    // 5. Insert WhatsApp Account (pending connection)
                    var waAccount = new WhatsAppAccount
                    {
                        BusinessId = business.Id,
                        Status = "Pending"
                    };
                    await _context.WhatsAppAccounts.AddAsync(waAccount);

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
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<UserRegisterResultDto?> RegisterUserAndBusinessWithSubscriptionAsync(
            RegisterDto registerDto, 
            byte[] passwordHash, 
            byte[] passwordSalt, 
            int activePlanId, 
            int allowedBranches, 
            int allowedStaff,
            string razorpayCustomerId, 
            string razorpaySubscriptionId, 
            DateTime expiresAt)
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

                    // 2. Insert Business (with subscription details)
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
                        PricesIncludeTax = registerDto.PricesIncludeTax,
                        ActivePlanId = activePlanId,
                        AllowedBranches = allowedBranches,
                        AllowedStaff = allowedStaff,
                        RazorpayCustomerId = razorpayCustomerId,
                        RazorpaySubscriptionId = razorpaySubscriptionId,
                        SubscriptionStatus = "Active",
                        SubscriptionExpiresAt = expiresAt
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

                    // 5. Insert WhatsApp Account (pending connection)
                    var waAccount = new WhatsAppAccount
                    {
                        BusinessId = business.Id,
                        Status = "Pending"
                    };
                    await _context.WhatsAppAccounts.AddAsync(waAccount);

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
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task AddRefreshTokenAsync(UserRefreshToken token)
        {
            // Clean up any expired refresh tokens for the user to prevent DB bloat
            var expired = await _context.UserRefreshTokens
                .Where(rt => rt.UserId == token.UserId && rt.ExpiryTime < DateTime.UtcNow)
                .ToListAsync();
            if (expired.Any())
            {
                _context.UserRefreshTokens.RemoveRange(expired);
            }

            await _context.UserRefreshTokens.AddAsync(token);
        }

        public async Task<UserRefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.UserRefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task RemoveRefreshTokenAsync(UserRefreshToken token)
        {
            _context.UserRefreshTokens.Remove(token);
            await Task.CompletedTask;
        }

        public async Task<PendingRegistration?> GetPendingRegistrationByEmailAsync(string email)
        {
            return await _context.PendingRegistrations
                .OrderByDescending(pr => pr.CreatedAt)
                .FirstOrDefaultAsync(pr => pr.Email == email && pr.Status == "PendingPayment");
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
