using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class ReportService : IReportService
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(BillingDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GstReportSummaryDto> GetGstReportAsync(int businessId, int? month = null, int? year = null, DateTime? startDate = null, DateTime? endDate = null, int? branchId = null)
        {
            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                throw new InvalidOperationException($"Business with ID {businessId} not found.");
            }

            // Determine date range
            DateTime start;
            DateTime end;

            if (startDate.HasValue && endDate.HasValue)
            {
                start = startDate.Value.Date;
                end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                int targetYear = year ?? DateTime.UtcNow.Year;
                int targetMonth = month ?? DateTime.UtcNow.Month;
                start = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                end = start.AddMonths(1).AddTicks(-1);
            }

            string periodLabel = $"{start.ToString("MMMM yyyy", CultureInfo.InvariantCulture)}";

            // Query Bills
            var billsQuery = _context.Bills
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Items)
                .Where(b => b.BusinessId == businessId)
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .Where(b => b.Status == "Paid" || b.Status == "Completed");

            if (branchId.HasValue)
            {
                billsQuery = billsQuery.Where(b => b.BranchId == branchId.Value);
            }

            var bills = await billsQuery.ToListAsync();

            var result = new GstReportSummaryDto
            {
                BusinessId = business.Id,
                BusinessName = business.LegalName,
                GstIn = business.GstIn ?? "Unregistered",
                PeriodLabel = periodLabel,
                StartDate = start,
                EndDate = end,
                TotalBillsCount = bills.Count
            };

            if (!bills.Any())
            {
                return result;
            }

            // Calculate overall bill totals
            result.TotalGrossSales = bills.Sum(b => b.Subtotal);
            result.TotalDiscountAmount = bills.Sum(b => b.DiscountAmount);
            result.TotalGrandSales = bills.Sum(b => b.TotalAmount);

            // Aggregate items & tax details
            var allItems = bills.SelectMany(b => b.Items).ToList();

            decimal totalTaxable = 0;
            decimal totalCGST = 0;
            decimal totalSGST = 0;
            decimal totalIGST = 0;
            decimal totalCess = 0;

            var slabMap = new Dictionary<decimal, TaxSlabSummaryDto>();
            var hsnMap = new Dictionary<string, HsnSacReportSummaryDto>();

            foreach (var b in bills)
            {
                bool isB2B = !string.IsNullOrWhiteSpace(b.Customer?.GstIn);
                decimal billTaxable = 0;
                decimal billGst = 0;

                foreach (var item in b.Items)
                {
                    decimal lineTaxable = item.TaxableValue > 0 ? item.TaxableValue : (item.UnitPrice * item.Quantity);
                    decimal cgst = item.CGSTAmount;
                    decimal sgst = item.SGSTAmount;
                    decimal igst = item.IGSTAmount;
                    decimal cess = item.CessAmount;

                    // Fallback tax calculation if snapshot wasn't stored on older bills
                    if (cgst == 0 && sgst == 0 && igst == 0 && item.TaxRate > 0)
                    {
                        decimal tax = Math.Round(lineTaxable * (item.TaxRate / 100.0m), 2);
                        cgst = Math.Round(tax / 2.0m, 2);
                        sgst = Math.Round(tax / 2.0m, 2);
                    }

                    decimal lineTaxTotal = cgst + sgst + igst + cess;

                    totalTaxable += lineTaxable;
                    totalCGST += cgst;
                    totalSGST += sgst;
                    totalIGST += igst;
                    totalCess += cess;

                    billTaxable += lineTaxable;
                    billGst += lineTaxTotal;

                    // Slab aggregation
                    decimal rate = item.TaxRate;
                    if (!slabMap.ContainsKey(rate))
                    {
                        slabMap[rate] = new TaxSlabSummaryDto { TaxRate = rate };
                    }
                    slabMap[rate].TaxableValue += lineTaxable;
                    slabMap[rate].CGSTAmount += cgst;
                    slabMap[rate].SGSTAmount += sgst;
                    slabMap[rate].IGSTAmount += igst;
                    slabMap[rate].TotalTaxAmount += lineTaxTotal;
                    slabMap[rate].LineItemsCount += 1;

                    // HSN / SAC aggregation
                    string code = !string.IsNullOrWhiteSpace(item.HSNCode) ? item.HSNCode : (!string.IsNullOrWhiteSpace(item.SACCode) ? item.SACCode : "OTHERS");
                    string desc = item.ServiceName;

                    if (!hsnMap.ContainsKey(code))
                    {
                        hsnMap[code] = new HsnSacReportSummaryDto
                        {
                            HsnSacCode = code,
                            Description = desc,
                            Type = !string.IsNullOrWhiteSpace(item.SACCode) ? "Services" : "Goods"
                        };
                    }
                    hsnMap[code].TotalQuantity += item.Quantity;
                    hsnMap[code].TaxableValue += lineTaxable;
                    hsnMap[code].CGSTAmount += cgst;
                    hsnMap[code].SGSTAmount += sgst;
                    hsnMap[code].IGSTAmount += igst;
                    hsnMap[code].TotalTaxAmount += lineTaxTotal;
                }

                if (isB2B)
                {
                    result.B2BBillsCount += 1;
                    result.B2BTaxableValue += billTaxable;
                    result.B2BGSTCollected += billGst;
                }
                else
                {
                    result.B2CBillsCount += 1;
                    result.B2CTaxableValue += billTaxable;
                    result.B2CGSTCollected += billGst;
                }

                // Payment mode distribution
                string method = string.IsNullOrEmpty(b.PaymentMethod) ? "Cash" : b.PaymentMethod;
                if (!result.PaymentMethodDistribution.ContainsKey(method))
                {
                    result.PaymentMethodDistribution[method] = 0;
                }
                result.PaymentMethodDistribution[method] += b.TotalAmount;
            }

            result.TotalTaxableValue = Math.Round(totalTaxable, 2);
            result.TotalCGST = Math.Round(totalCGST, 2);
            result.TotalSGST = Math.Round(totalSGST, 2);
            result.TotalIGST = Math.Round(totalIGST, 2);
            result.TotalCess = Math.Round(totalCess, 2);
            result.TotalGSTCollected = Math.Round(totalCGST + totalSGST + totalIGST + totalCess, 2);

            result.TaxSlabs = slabMap.Values.OrderBy(s => s.TaxRate).ToList();
            result.HsnSacBreakdown = hsnMap.Values.OrderByDescending(h => h.TaxableValue).ToList();

            // Daily sales trends
            var dailyMap = bills.GroupBy(b => b.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyGstSalesTrendDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    BillsCount = g.Count(),
                    TaxableValue = g.Sum(b => b.Items.Sum(i => i.TaxableValue > 0 ? i.TaxableValue : i.UnitPrice * i.Quantity)),
                    GSTCollected = g.Sum(b => b.TaxAmount),
                    TotalSales = g.Sum(b => b.TotalAmount)
                }).ToList();

            result.DailyTrends = dailyMap;

            return result;
        }

        public async Task<byte[]> ExportGstReportCsvAsync(int businessId, int? month = null, int? year = null, DateTime? startDate = null, DateTime? endDate = null, int? branchId = null)
        {
            var report = await GetGstReportAsync(businessId, month, year, startDate, endDate, branchId);

            var sb = new StringBuilder();

            // Header Section
            sb.AppendLine($"BILLCOM - GST TAX SUMMARY REPORT");
            sb.AppendLine($"Business Name,{EscapeCsv(report.BusinessName)}");
            sb.AppendLine($"GSTIN,{EscapeCsv(report.GstIn)}");
            sb.AppendLine($"Period,{EscapeCsv(report.PeriodLabel)}");
            sb.AppendLine($"Generated On,{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} UTC");
            sb.AppendLine();

            // Summary Totals
            sb.AppendLine($"SUMMARY METRICS");
            sb.AppendLine($"Total Bills Issued,{report.TotalBillsCount}");
            sb.AppendLine($"Total Gross Sales (₹),{report.TotalGrossSales:F2}");
            sb.AppendLine($"Total Discount (₹),{report.TotalDiscountAmount:F2}");
            sb.AppendLine($"Total Taxable Value (₹),{report.TotalTaxableValue:F2}");
            sb.AppendLine($"Total CGST (₹),{report.TotalCGST:F2}");
            sb.AppendLine($"Total SGST (₹),{report.TotalSGST:F2}");
            sb.AppendLine($"Total IGST (₹),{report.TotalIGST:F2}");
            sb.AppendLine($"Total Cess (₹),{report.TotalCess:F2}");
            sb.AppendLine($"Total GST Collected (₹),{report.TotalGSTCollected:F2}");
            sb.AppendLine($"Grand Total Sales (₹),{report.TotalGrandSales:F2}");
            sb.AppendLine();

            // B2B vs B2C
            sb.AppendLine($"B2B vs B2C TAX BREAKDOWN");
            sb.AppendLine($"Type,Bills Count,Taxable Value (₹),GST Collected (₹)");
            sb.AppendLine($"B2B Registered Sales,{report.B2BBillsCount},{report.B2BTaxableValue:F2},{report.B2BGSTCollected:F2}");
            sb.AppendLine($"B2C Retail Sales,{report.B2CBillsCount},{report.B2CTaxableValue:F2},{report.B2CGSTCollected:F2}");
            sb.AppendLine();

            // Tax Slab Breakdown Table
            sb.AppendLine($"GST SLAB-WISE BREAKDOWN");
            sb.AppendLine($"GST Rate (%),Taxable Value (₹),CGST (₹),SGST (₹),IGST (₹),Total Tax (₹)");
            foreach (var slab in report.TaxSlabs)
            {
                sb.AppendLine($"{slab.TaxRate:F2},{slab.TaxableValue:F2},{slab.CGSTAmount:F2},{slab.SGSTAmount:F2},{slab.IGSTAmount:F2},{slab.TotalTaxAmount:F2}");
            }
            sb.AppendLine();

            // HSN / SAC Table
            sb.AppendLine($"HSN / SAC SUMMARY REPORT");
            sb.AppendLine($"HSN/SAC Code,Description,Type,Total Qty,Taxable Value (₹),CGST (₹),SGST (₹),IGST (₹),Total Tax (₹)");
            foreach (var hsn in report.HsnSacBreakdown)
            {
                sb.AppendLine($"{EscapeCsv(hsn.HsnSacCode)},{EscapeCsv(hsn.Description)},{hsn.Type},{hsn.TotalQuantity},{hsn.TaxableValue:F2},{hsn.CGSTAmount:F2},{hsn.SGSTAmount:F2},{hsn.IGSTAmount:F2},{hsn.TotalTaxAmount:F2}");
            }
            sb.AppendLine();

            // Daily Trends Table
            sb.AppendLine($"DAILY SALES & GST TREND");
            sb.AppendLine($"Date,Bills Count,Taxable Value (₹),GST Collected (₹),Total Sales (₹)");
            foreach (var d in report.DailyTrends)
            {
                sb.AppendLine($"{d.Date},{d.BillsCount},{d.TaxableValue:F2},{d.GSTCollected:F2},{d.TotalSales:F2}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }
    }
}
