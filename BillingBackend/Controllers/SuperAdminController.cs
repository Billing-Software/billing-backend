using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace BillingBackend.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SuperAdminController : ControllerBase
    {
        private readonly BillingDbContext _context;

        public SuperAdminController(BillingDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var totalClients = await _context.Businesses.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var totalBills = await _context.Bills.CountAsync();
                var totalRevenue = await _context.Bills.SumAsync(b => (double)b.TotalAmount);
                var totalBranches = await _context.Branches.CountAsync();
                var totalCustomers = await _context.Customers.CountAsync();
                var totalStaff = await _context.StaffMembers.CountAsync();
                var totalServices = await _context.Services.CountAsync();

                // Recent signups (businesses)
                var recentClients = await _context.Businesses
                    .Include(b => b.Owner)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new {
                        b.Id,
                        b.LegalName,
                        OwnerName = b.Owner.Username,
                        OwnerEmail = b.Owner.Email,
                        b.City,
                        b.CreatedAt,
                        b.IsSuspended
                    })
                    .ToListAsync();

                // Recent bills across all clients
                var recentBills = await _context.Bills
                    .Include(b => b.Business)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new {
                        b.Id,
                        b.BillNumber,
                        BusinessName = b.Business.LegalName,
                        GrandTotal = b.TotalAmount,
                        b.CreatedAt,
                        b.Status
                    })
                    .ToListAsync();

                // Client count signup trends
                var signups = await _context.Businesses
                    .Select(b => new { b.CreatedAt })
                    .ToListAsync();

                var signupTrend = signups
                    .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Period)
                    .ToList();

                // Revenue trends
                var revenues = await _context.Bills
                    .Select(b => new { b.CreatedAt, b.TotalAmount })
                    .ToListAsync();

                var revenueTrend = revenues
                    .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Total = g.Sum(x => (double)x.TotalAmount)
                    })
                    .OrderBy(x => x.Period)
                    .ToList();

                // Top clients by billing volume
                var topClientsQuery = await _context.Businesses
                    .Select(b => new {
                        b.Id,
                        b.LegalName,
                        BillCount = b.Bills.Count,
                        TotalRevenue = b.Bills.Sum(x => (double)x.TotalAmount),
                        b.IsSuspended
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(5)
                    .ToListAsync();

                return Ok(new {
                    TotalClients = totalClients,
                    TotalUsers = totalUsers,
                    TotalBills = totalBills,
                    TotalRevenue = totalRevenue,
                    TotalBranches = totalBranches,
                    TotalCustomers = totalCustomers,
                    TotalStaff = totalStaff,
                    TotalServices = totalServices,
                    RecentClients = recentClients,
                    RecentBills = recentBills,
                    SignupTrend = signupTrend,
                    RevenueTrend = revenueTrend,
                    TopClients = topClientsQuery
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching dashboard statistics.", error = ex.Message });
            }
        }

        [HttpGet("payments")]
        public async Task<IActionResult> GetPaymentStats()
        {
            try
            {
                var totalSubscriptions = await _context.Businesses.CountAsync(b => b.SubscriptionStatus == "Active");
                var successfulPayments = await _context.PaymentTransactions.CountAsync(t => t.Status == "Captured");
                var failedPayments = await _context.PendingRegistrations.CountAsync(p => p.Status == "Failed");
                var pendingPayments = await _context.PendingRegistrations.CountAsync(p => p.Status == "PendingPayment" && p.ExpiresAt >= DateTime.UtcNow);
                var abandonedCheckouts = await _context.PendingRegistrations.CountAsync(p => p.Status == "PendingPayment" && p.ExpiresAt < DateTime.UtcNow);

                var transactionsList = await _context.PaymentTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new {
                        t.Id,
                        t.BusinessId,
                        BusinessName = _context.Businesses.Where(b => b.Id == t.BusinessId).Select(b => b.LegalName).FirstOrDefault() ?? "Unknown Store",
                        OwnerEmail = _context.Businesses.Where(b => b.Id == t.BusinessId).Include(b => b.Owner).Select(b => b.Owner.Email).FirstOrDefault() ?? "N/A",
                        t.RazorpayPaymentId,
                        t.RazorpaySubscriptionId,
                        t.Amount,
                        t.Status,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var pendingList = await _context.PendingRegistrations
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new {
                        p.Id,
                        p.Token,
                        p.Username,
                        p.Email,
                        p.LegalName,
                        p.Phone,
                        p.GstIn,
                        PlanName = _context.SubscriptionPlans.Where(sp => sp.Id == p.SelectedPlanId).Select(sp => sp.Name).FirstOrDefault() ?? "Starter Plan",
                        p.Status,
                        p.RazorpaySubscriptionId,
                        p.CreatedAt,
                        p.ExpiresAt
                    })
                    .ToListAsync();

                return Ok(new {
                    TotalActiveSubscriptions = totalSubscriptions,
                    SuccessfulPaymentsCount = successfulPayments,
                    FailedPaymentsCount = failedPayments,
                    PendingPaymentsCount = pendingPayments,
                    AbandonedCheckoutsCount = abandonedCheckouts,
                    Transactions = transactionsList,
                    PendingRegistrations = pendingList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching payment statistics.", error = ex.Message });
            }
        }

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            try
            {
                var clients = await _context.Businesses
                    .Include(b => b.Owner)
                    .Select(b => new {
                        b.Id,
                        b.LegalName,
                        b.TradingName,
                        b.Address,
                        b.City,
                        b.State,
                        b.PostalCode,
                        b.Country,
                        b.Phone,
                        b.Email,
                        b.Website,
                        b.GstIn,
                        b.DefaultTaxRate,
                        b.PricesIncludeTax,
                        b.CreatedAt,
                        b.IsSuspended,
                        b.OwnerId,
                        OwnerUsername = b.Owner.Username,
                        OwnerEmail = b.Owner.Email,
                        BranchCount = b.Branches.Count,
                        StaffCount = b.StaffMembers.Count,
                        CustomerCount = b.Customers.Count,
                        BillCount = b.Bills.Count,
                        TotalRevenue = b.Bills.Sum(x => (double)x.TotalAmount)
                    })
                    .ToListAsync();

                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving clients list.", error = ex.Message });
            }
        }

        [HttpPost("clients")]
        public async Task<IActionResult> CreateClient(RegisterDto registerDto)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == registerDto.Username.ToLower()))
                {
                    return BadRequest("Username already exists.");
                }

                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower()))
                {
                    return BadRequest("Email already exists.");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    using var hmac = new HMACSHA512();
                    var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
                    var passwordSalt = hmac.Key;

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
                        TradingName = registerDto.TradingName ?? registerDto.LegalName,
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
                        IsSuspended = false
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

                    return Ok(new {
                        business.Id,
                        business.LegalName,
                        OwnerId = user.Id,
                        user.Username,
                        user.Email,
                        business.CreatedAt
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Failed to create client: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error creating client.", error = ex.Message });
            }
        }

        [HttpPut("clients/{id}")]
        public async Task<IActionResult> UpdateClient(int id, BusinessDto dto)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null) return NotFound("Client not found.");

                business.LegalName = dto.LegalName;
                business.TradingName = dto.TradingName;
                business.Address = dto.Address;
                business.City = dto.City;
                business.State = dto.State;
                business.PostalCode = dto.PostalCode;
                business.Country = dto.Country ?? "India";
                business.Phone = dto.Phone;
                business.Email = dto.Email;
                business.Website = dto.Website;
                business.GstIn = dto.GstIn;
                business.DefaultTaxRate = dto.DefaultTaxRate;
                business.PricesIncludeTax = dto.PricesIncludeTax;
                business.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(business);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating client details.", error = ex.Message });
            }
        }

        [HttpPost("clients/{id}/status")]
        public async Task<IActionResult> ToggleSuspension(int id)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null) return NotFound("Client not found.");

                business.IsSuspended = !business.IsSuspended;
                business.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { id = business.Id, isSuspended = business.IsSuspended });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error toggling client suspension status.", error = ex.Message });
            }
        }

        [HttpDelete("clients/{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            try
            {
                var business = await _context.Businesses
                    .Include(b => b.Owner)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (business == null) return NotFound("Client not found.");

                // Also delete associated PendingRegistrations
                var emailsToDelete = new List<string>();
                if (business.Owner != null && !string.IsNullOrEmpty(business.Owner.Email))
                {
                    emailsToDelete.Add(business.Owner.Email);
                }
                if (!string.IsNullOrEmpty(business.Email))
                {
                    emailsToDelete.Add(business.Email);
                }

                if (emailsToDelete.Any())
                {
                    var uniqueEmails = emailsToDelete.Distinct().ToList();
                    var pendings = await _context.PendingRegistrations
                        .Where(p => uniqueEmails.Contains(p.Email))
                        .ToListAsync();
                    if (pendings.Any())
                    {
                        _context.PendingRegistrations.RemoveRange(pendings);
                    }
                }

                _context.Businesses.Remove(business);
                if (business.Owner != null)
                {
                    _context.Users.Remove(business.Owner);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting client record.", error = ex.Message });
            }
        }
    }
}
