using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly BillingDbContext _context;

        public StaffRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<StaffMember?> GetByIdAsync(int businessId, int id)
        {
            return await _context.StaffMembers
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.Id == id);
        }

        public async Task<StaffMember?> GetByUserIdAsync(int userId)
        {
            return await _context.StaffMembers
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<StaffMember?> GetByUserIdAndBusinessIdAsync(int userId, int businessId)
        {
            return await _context.StaffMembers
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.BusinessId == businessId);
        }

        public async Task<IEnumerable<StaffMember>> GetByBusinessIdAsync(int businessId)
        {
            return await _context.StaffMembers
                .Include(s => s.Branch)
                .Where(s => s.BusinessId == businessId)
                .ToListAsync();
        }

        public async Task<StaffMember> AddAsync(StaffMember staff, string password)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create associated User record
                    using var hmac = new HMACSHA512();
                    var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    var passwordSalt = hmac.Key;

                    var user = new User
                    {
                        Username = staff.Contact ?? staff.EmpCode,
                        Email = staff.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com",
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = staff.Role,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();

                    // Assign the created UserId
                    staff.UserId = user.Id;
                    staff.CreatedAt = DateTime.UtcNow;

                    await _context.StaffMembers.AddAsync(staff);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return staff;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<StaffMember> UpdateAsync(StaffMember staff, string? password)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existing = await _context.StaffMembers.FirstOrDefaultAsync(s => s.BusinessId == staff.BusinessId && s.Id == staff.Id);
                    if (existing == null)
                    {
                        throw new KeyNotFoundException($"Staff member with ID {staff.Id} for Business {staff.BusinessId} not found");
                    }

                    if (existing.UserId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(existing.UserId.Value);
                        if (user != null)
                        {
                            user.Username = staff.Contact ?? existing.Contact ?? staff.EmpCode;
                            user.Email = staff.Contact ?? existing.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com";
                            user.Role = staff.Role;

                            if (!string.IsNullOrEmpty(password))
                            {
                                using var hmac = new HMACSHA512();
                                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                                user.PasswordSalt = hmac.Key;
                            }
                            _context.Users.Update(user);
                        }
                    }
                    else
                    {
                        using var hmac = new HMACSHA512();
                        var pass = string.IsNullOrEmpty(password) ? "123456" : password;
                        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(pass));
                        var passwordSalt = hmac.Key;

                        var user = new User
                        {
                            Username = staff.Contact ?? staff.EmpCode,
                            Email = staff.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com",
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt,
                            Role = staff.Role,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                        existing.UserId = user.Id;
                    }

                    existing.Name = staff.Name;
                    existing.EmpCode = staff.EmpCode;
                    existing.Contact = staff.Contact;
                    existing.Role = staff.Role;
                    existing.Status = staff.Status;
                    existing.BranchId = staff.BranchId;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existing;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existing = await _context.StaffMembers.FirstOrDefaultAsync(s => s.BusinessId == businessId && s.Id == id);
                    if (existing == null)
                    {
                        return false;
                    }

                    var userId = existing.UserId;

                    _context.StaffMembers.Remove(existing);
                    await _context.SaveChangesAsync();

                    if (userId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(userId.Value);
                        if (user != null)
                        {
                            _context.Users.Remove(user);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
