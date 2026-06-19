using BillingBackend.Data;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_GetDashboardData";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    var pBusinessId = command.CreateParameter();
                    pBusinessId.ParameterName = "@BusinessId";
                    pBusinessId.Value = businessId;
                    command.Parameters.Add(pBusinessId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // 1. Summary Metrics
                        if (await reader.ReadAsync())
                        {
                            data.Summary = new DashboardSummaryDto
                            {
                                TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                                TotalBills = Convert.ToInt32(reader["TotalBills"]),
                                TotalCustomers = Convert.ToInt32(reader["TotalCustomers"]),
                                LowStockCount = Convert.ToInt32(reader["LowStockCount"])
                            };
                        }

                        // 2. Recent Bills
                        if (await reader.NextResultAsync())
                        {
                            data.RecentBills = new List<DashboardRecentBillDto>();
                            while (await reader.ReadAsync())
                            {
                                data.RecentBills.Add(new DashboardRecentBillDto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    BillNumber = Convert.ToString(reader["BillNumber"]) ?? string.Empty,
                                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                    Status = Convert.ToString(reader["Status"]) ?? string.Empty,
                                    CustomerName = Convert.ToString(reader["CustomerName"]) ?? string.Empty
                                });
                            }
                        }

                        // 3. Top Selling Services
                        if (await reader.NextResultAsync())
                        {
                            data.TopServices = new List<DashboardTopServiceDto>();
                            while (await reader.ReadAsync())
                            {
                                data.TopServices.Add(new DashboardTopServiceDto
                                {
                                    ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                    ServiceName = Convert.ToString(reader["ServiceName"]) ?? string.Empty,
                                    TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                                    TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"])
                                });
                            }
                        }

                        // 4. Low Stock Items
                        if (await reader.NextResultAsync())
                        {
                            data.LowStockItems = new List<DashboardLowStockDto>();
                            while (await reader.ReadAsync())
                            {
                                data.LowStockItems.Add(new DashboardLowStockDto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = Convert.ToString(reader["Name"]) ?? string.Empty,
                                    SKU = Convert.ToString(reader["SKU"]) ?? string.Empty,
                                    CurrentStock = Convert.ToInt32(reader["CurrentStock"]),
                                    ReorderLevel = Convert.ToInt32(reader["ReorderLevel"]),
                                    Unit = Convert.ToString(reader["Unit"]) ?? string.Empty
                                });
                            }
                        }

                        // 5. Sales Trend
                        if (await reader.NextResultAsync())
                        {
                            data.SalesTrend = new List<DashboardSalesTrendDto>();
                            while (await reader.ReadAsync())
                            {
                                data.SalesTrend.Add(new DashboardSalesTrendDto
                                {
                                    SalesDate = Convert.ToDateTime(reader["SalesDate"]),
                                    BillsCount = Convert.ToInt32(reader["BillsCount"]),
                                    DailyRevenue = Convert.ToDecimal(reader["DailyRevenue"])
                                });
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }

            return data;
        }
    }
}
