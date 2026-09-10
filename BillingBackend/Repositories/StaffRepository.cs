using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static string NormalizeStaffRole(string? role)
        {
            if (!string.IsNullOrWhiteSpace(role) && PasswordPolicy.StaffRoles.Contains(role.Trim()))
                return role.Trim();
            return "Staff";
        }

        public async Task<StaffMember> AddAsync(StaffMember staff, string password)
        {
            var (pwOk, pwError) = PasswordPolicy.Validate(password);
            if (!pwOk)
                throw new InvalidOperationException(pwError ?? "Staff password does not meet policy. Provide a strong temporary password.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    PasswordHasher.CreateHash(password, out var passwordHash, out var passwordSalt);

                    var safeRole = NormalizeStaffRole(staff.Role);
                    var user = new User
                    {
                        Username = staff.Contact ?? staff.EmpCode,
                        Email = staff.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com",
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Role = safeRole,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();

                    // Assign the created UserId
                    staff.UserId = user.Id;
                    staff.Role = safeRole;
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

                    var safeRole = NormalizeStaffRole(staff.Role);

                    if (existing.UserId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(existing.UserId.Value);
                        if (user != null)
                        {
                            user.Username = staff.Contact ?? existing.Contact ?? staff.EmpCode;
                            user.Email = staff.Contact ?? existing.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com";
                            user.Role = safeRole;

                            if (!string.IsNullOrEmpty(password))
                            {
                                var (pwOk, pwError) = PasswordPolicy.Validate(password);
                                if (!pwOk)
                                    throw new InvalidOperationException(pwError ?? "Password does not meet policy.");
                                PasswordHasher.CreateHash(password, out var nh, out var ns);
                                user.PasswordHash = nh;
                                user.PasswordSalt = ns;
                            }
                            _context.Users.Update(user);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(password))
                            throw new InvalidOperationException("Password is required when creating login for existing staff.");
                        var (pwOk, pwError) = PasswordPolicy.Validate(password);
                        if (!pwOk)
                            throw new InvalidOperationException(pwError ?? "Password does not meet policy.");
                        PasswordHasher.CreateHash(password, out var passwordHash, out var passwordSalt);

                        var user = new User
                        {
                            Username = staff.Contact ?? staff.EmpCode,
                            Email = staff.Contact ?? $"{staff.EmpCode.ToLower()}@business{staff.BusinessId}.com",
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt,
                            Role = safeRole,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                        existing.UserId = user.Id;
                    }

                    existing.Name = staff.Name;
                    existing.EmpCode = staff.EmpCode;
                    existing.Contact = staff.Contact;
                    existing.Role = safeRole;
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
