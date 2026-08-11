using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly BillingDbContext _context;

        public BillRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<Bill?> GetByIdAsync(int businessId, int id)
        {
            return await _context.Bills
                .Include(b => b.Items)
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.CreatedByStaff)
                .FirstOrDefaultAsync(b => b.BusinessId == businessId && b.Id == id);
        }

        public async Task<Bill?> GetByIdempotencyKeyAsync(int businessId, string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey)) return null;

            return await _context.Bills
                .Include(b => b.Items)
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.CreatedByStaff)
                .FirstOrDefaultAsync(b => b.BusinessId == businessId && b.IdempotencyKey == idempotencyKey);
        }

        public async Task<IEnumerable<Bill>> GetByBusinessIdAsync(
            int businessId,
            int? customerId = null,
            int? staffId = null,
            int? branchId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null)
        {
            var query = _context.Bills
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.CreatedByStaff)
                .Where(b => b.BusinessId == businessId);

            if (customerId.HasValue)
            {
                query = query.Where(b => b.CustomerId == customerId.Value);
            }

            if (staffId.HasValue)
            {
                query = query.Where(b => b.CreatedByStaffId == staffId.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(b => b.BranchId == branchId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (minAmount.HasValue)
            {
                query = query.Where(b => b.TotalAmount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(b => b.TotalAmount <= maxAmount.Value);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Bill> AddAsync(Bill bill, string itemsJson)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    bill.CreatedAt = DateTime.UtcNow;

                    // Server-side Bill Number generation to prevent client side manipulation and collisions.
                    // Format: INV-YYYYMMDD-XXXX where XXXX is a daily sequential index starting at 0001
                    var today = DateTime.UtcNow.Date;
                    var todayStr = today.ToString("yyyyMMdd");
                    
                    var dailyCount = await _context.Bills
                        .Where(b => b.BusinessId == bill.BusinessId && b.CreatedAt >= today)
                        .CountAsync();

                    bill.BillNumber = $"INV-{todayStr}-{(dailyCount + 1).ToString("D4")}";
                    
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
                    };
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<BillItem>>(itemsJson, options) ?? new List<BillItem>();
                    
                    bill.Items = new List<BillItem>();
                    foreach (var item in items)
                    {
                        item.Bill = bill;
                        if (string.IsNullOrEmpty(item.ItemType))
                        {
                            item.ItemType = "Service";
                        }
                        bill.Items.Add(item);
                    }

                    await _context.Bills.AddAsync(bill);
                    await _context.SaveChangesAsync();

                    if (bill.CreatedByStaffId.HasValue)
                    {
                        var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.Id == bill.CreatedByStaffId.Value && s.BusinessId == bill.BusinessId);
                        if (staff != null)
                        {
                            staff.TotalBills += 1;
                            staff.RevenueGenerated += bill.TotalAmount;
                            staff.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return bill;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task UpdateAsync(Bill bill)
        {
            bill.UpdatedAt = DateTime.UtcNow;
            _context.Bills.Update(bill);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var bill = await _context.Bills
                        .Include(b => b.Items)
                        .FirstOrDefaultAsync(b => b.BusinessId == businessId && b.Id == id);
                        
                    if (bill == null)
                    {
                        return false;
                    }

                    var staffId = bill.CreatedByStaffId;
                    var totalAmount = bill.TotalAmount;

                    _context.BillItems.RemoveRange(bill.Items);
                    _context.Bills.Remove(bill);
                    await _context.SaveChangesAsync();

                    if (staffId.HasValue)
                    {
                        var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.Id == staffId.Value && s.BusinessId == businessId);
                        if (staff != null)
                        {
                            staff.TotalBills = Math.Max(0, staff.TotalBills - 1);
                            staff.RevenueGenerated = Math.Max(0, staff.RevenueGenerated - totalAmount);
                            staff.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
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
