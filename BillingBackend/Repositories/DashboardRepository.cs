using BillingBackend.Data;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly BillingDbContext _context;

        public DashboardRepository(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(int businessId)
        {
            var data = new DashboardDataDto();

            // 1. Summary Metrics
            var billsQuery = _context.Bills.Where(b => b.BusinessId == businessId);
            
            var totalRevenue = await billsQuery.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            var totalBills = await billsQuery.CountAsync();
            var totalCustomers = await _context.Customers.CountAsync(c => c.BusinessId == businessId);
            var lowStockCount = await _context.InventoryItems.CountAsync(i => i.BusinessId == businessId && i.CurrentStock <= i.ReorderLevel);

            data.Summary = new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalBills = totalBills,
                TotalCustomers = totalCustomers,
                LowStockCount = lowStockCount
            };

            // 2. Recent Bills (Top 5)
            data.RecentBills = await billsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new DashboardRecentBillDto
                {
                    Id = b.Id,
                    BillNumber = b.BillNumber,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt,
                    Status = b.Status,
                    CustomerName = b.Customer.Name,
                    StaffName = b.CreatedByStaff != null ? b.CreatedByStaff.Name : "Owner"
                })
                .ToListAsync();

            // 3. Top Selling Services (Top 5)
            data.TopServices = await _context.BillItems
                .Where(bi => bi.Bill.BusinessId == businessId)
                .GroupBy(bi => new { bi.ServiceId, bi.ServiceName })
                .Select(g => new DashboardTopServiceDto
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.ServiceName,
                    TotalQuantity = g.Sum(bi => bi.Quantity),
                    TotalRevenue = g.Sum(bi => bi.LineTotal)
                })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(5)
                .ToListAsync();

            // 4. Low Stock Items (Top 5)
            data.LowStockItems = await _context.InventoryItems
                .Where(i => i.BusinessId == businessId && i.CurrentStock <= i.ReorderLevel)
                .OrderBy(i => i.CurrentStock)
                .Take(5)
                .Select(i => new DashboardLowStockDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    SKU = i.SKU,
                    CurrentStock = i.CurrentStock,
                    ReorderLevel = i.ReorderLevel,
                    Unit = i.Unit
                })
                .ToListAsync();

            // 5. Sales Trend (Last 30 Days)
            var minDate = DateTime.UtcNow.Date.AddDays(-30);
            var trendBills = await billsQuery
                .Where(b => b.CreatedAt >= minDate)
                .ToListAsync();
                
            data.SalesTrend = trendBills
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new DashboardSalesTrendDto
                {
                    SalesDate = g.Key,
                    BillsCount = g.Count(),
                    DailyRevenue = g.Sum(b => b.TotalAmount)
                })
                .OrderBy(t => t.SalesDate)
                .ToList();

            return data;
        }

        public async Task<DashboardDataDto> GetStaffDashboardDataAsync(int businessId, int userId)
        {
            var data = new DashboardDataDto();

            var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.BusinessId == businessId && s.UserId == userId);
            if (staff == null)
            {
                data.Summary = new DashboardSummaryDto();
                data.RecentBills = new List<DashboardRecentBillDto>();
                data.TopServices = new List<DashboardTopServiceDto>();
                data.LowStockItems = new List<DashboardLowStockDto>();
                data.SalesTrend = new List<DashboardSalesTrendDto>();
                return data;
            }

            var billsQuery = _context.Bills.Where(b => b.BusinessId == businessId && b.CreatedByStaffId == staff.Id);
            
            var totalRevenue = await billsQuery.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            var totalBills = await billsQuery.CountAsync();
            var totalCustomers = await billsQuery.Select(b => b.CustomerId).Distinct().CountAsync();
            var lowStockCount = await _context.InventoryItems.CountAsync(i => i.BusinessId == businessId && i.CurrentStock <= i.ReorderLevel);

            data.Summary = new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalBills = totalBills,
                TotalCustomers = totalCustomers,
                LowStockCount = lowStockCount
            };

            data.RecentBills = await billsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new DashboardRecentBillDto
                {
                    Id = b.Id,
                    BillNumber = b.BillNumber,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt,
                    Status = b.Status,
                    CustomerName = b.Customer.Name,
                    StaffName = staff.Name
                })
                .ToListAsync();

            data.TopServices = await _context.BillItems
                .Where(bi => bi.Bill.BusinessId == businessId && bi.Bill.CreatedByStaffId == staff.Id)
                .GroupBy(bi => new { bi.ServiceId, bi.ServiceName })
                .Select(g => new DashboardTopServiceDto
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.ServiceName,
                    TotalQuantity = g.Sum(bi => bi.Quantity),
                    TotalRevenue = g.Sum(bi => bi.LineTotal)
                })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(5)
                .ToListAsync();

            data.LowStockItems = new List<DashboardLowStockDto>();

            var minDate = DateTime.UtcNow.Date.AddDays(-30);
            var trendBills = await billsQuery
                .Where(b => b.CreatedAt >= minDate)
                .ToListAsync();
                
            data.SalesTrend = trendBills
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new DashboardSalesTrendDto
                {
                    SalesDate = g.Key,
                    BillsCount = g.Count(),
                    DailyRevenue = g.Sum(b => b.TotalAmount)
                })
                .OrderBy(t => t.SalesDate)
                .ToList();

            return data;
        }
    }
}
