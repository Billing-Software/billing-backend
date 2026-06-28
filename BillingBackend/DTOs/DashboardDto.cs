using System;
using System.Collections.Generic;

namespace BillingBackend.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBills { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockCount { get; set; }
    }

    public class DashboardRecentBillDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? StaffName { get; set; }
    }

    public class DashboardTopServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DashboardLowStockDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class DashboardSalesTrendDto
    {
        public DateTime SalesDate { get; set; }
        public int BillsCount { get; set; }
        public decimal DailyRevenue { get; set; }
    }

    public class DashboardDataDto
    {
        public DashboardSummaryDto Summary { get; set; } = new DashboardSummaryDto();
        public List<DashboardRecentBillDto> RecentBills { get; set; } = new List<DashboardRecentBillDto>();
        public List<DashboardTopServiceDto> TopServices { get; set; } = new List<DashboardTopServiceDto>();
        public List<DashboardLowStockDto> LowStockItems { get; set; } = new List<DashboardLowStockDto>();
        public List<DashboardSalesTrendDto> SalesTrend { get; set; } = new List<DashboardSalesTrendDto>();
    }
}
