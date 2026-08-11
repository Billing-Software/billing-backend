using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class TaxService : ITaxService
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<TaxService> _logger;

        public TaxService(BillingDbContext context, ILogger<TaxService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TaxCategoryDto>> GetTaxCategoriesAsync(int businessId)
        {
            var categories = await _context.TaxCategories
                .AsNoTracking()
                .Where(tc => tc.BusinessId == null || tc.BusinessId == businessId)
                .Where(tc => tc.IsActive)
                .OrderBy(tc => tc.Name)
                .ToListAsync();

            return categories.Select(c => new TaxCategoryDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                Name = c.Name,
                TaxType = c.TaxType,
                HSNCode = c.HSNCode,
                SACCode = c.SACCode,
                GSTPercentage = c.GSTPercentage,
                CGSTPercentage = c.CGSTPercentage,
                SGSTPercentage = c.SGSTPercentage,
                IGSTPercentage = c.IGSTPercentage,
                CessPercentage = c.CessPercentage,
                IsActive = c.IsActive
            });
        }

        public async Task<TaxCategoryDto> CreateTaxCategoryAsync(int businessId, TaxCategoryDto dto)
        {
            var entity = new TaxCategory
            {
                BusinessId = businessId,
                Name = dto.Name,
                TaxType = dto.TaxType,
                HSNCode = dto.HSNCode,
                SACCode = dto.SACCode,
                GSTPercentage = dto.GSTPercentage,
                CGSTPercentage = dto.GSTPercentage / 2.0m,
                SGSTPercentage = dto.GSTPercentage / 2.0m,
                IGSTPercentage = dto.GSTPercentage,
                CessPercentage = dto.CessPercentage,
                IsActive = true
            };

            _context.TaxCategories.Add(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            dto.BusinessId = entity.BusinessId;
            return dto;
        }

        public async Task<IEnumerable<HSNMaster>> SearchHSNAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.HSNMasters
                    .AsNoTracking()
                    .Where(h => h.IsActive)
                    .Take(20)
                    .ToListAsync();
            }

            var q = query.Trim().ToLower();

            return await _context.HSNMasters
                .AsNoTracking()
                .Where(h => h.IsActive && (
                    h.Code.ToLower().Contains(q) ||
                    h.Description.ToLower().Contains(q) ||
                    (h.SearchTerms != null && h.SearchTerms.ToLower().Contains(q))
                ))
                .Take(20)
                .ToListAsync();
        }

        public async Task<IEnumerable<SACMaster>> SearchSACAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.SACMasters
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .Take(20)
                    .ToListAsync();
            }

            var q = query.Trim().ToLower();

            return await _context.SACMasters
                .AsNoTracking()
                .Where(s => s.IsActive && (
                    s.Code.ToLower().Contains(q) ||
                    s.Description.ToLower().Contains(q) ||
                    (s.SearchTerms != null && s.SearchTerms.ToLower().Contains(q))
                ))
                .Take(20)
                .ToListAsync();
        }

        public async Task<TaxCalculationResultDto> CalculateTaxAsync(int businessId, TaxCalculationRequestDto request)
        {
            var business = await _context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId);
            string supplierState = request.SupplierState ?? business?.RegisteredState ?? business?.State ?? "Andhra Pradesh";
            string customerState = request.CustomerState ?? supplierState;

            bool isInterState = !string.Equals(supplierState.Trim(), customerState.Trim(), StringComparison.OrdinalIgnoreCase);

            var result = new TaxCalculationResultDto
            {
                IsInterState = isInterState
            };

            var categories = await _context.TaxCategories
                .AsNoTracking()
                .Where(tc => tc.BusinessId == null || tc.BusinessId == businessId)
                .ToListAsync();

            decimal totalTaxable = 0;
            decimal totalCGST = 0;
            decimal totalSGST = 0;
            decimal totalIGST = 0;
            decimal totalCess = 0;

            foreach (var item in request.Items)
            {
                decimal grossPrice = item.UnitPrice * item.Quantity;
                decimal taxable = Math.Max(0, grossPrice - item.DiscountAmount);

                TaxCategory? category = null;
                if (item.TaxCategoryId.HasValue)
                {
                    category = categories.FirstOrDefault(c => c.Id == item.TaxCategoryId.Value);
                }

                decimal gstRate = category?.GSTPercentage ?? business?.DefaultTaxRate ?? 18.00m;
                decimal cessRate = category?.CessPercentage ?? 0.00m;
                string? hsn = category?.HSNCode ?? item.CustomHSNSAC;
                string? sac = category?.SACCode ?? item.CustomHSNSAC;

                decimal cgstRate = 0, sgstRate = 0, igstRate = 0;
                decimal cgstAmount = 0, sgstAmount = 0, igstAmount = 0;

                if (isInterState)
                {
                    igstRate = gstRate;
                    igstAmount = Math.Round(taxable * (igstRate / 100.0m), 2);
                }
                else
                {
                    cgstRate = Math.Round(gstRate / 2.0m, 2);
                    sgstRate = Math.Round(gstRate / 2.0m, 2);
                    cgstAmount = Math.Round(taxable * (cgstRate / 100.0m), 2);
                    sgstAmount = Math.Round(taxable * (sgstRate / 100.0m), 2);
                }

                decimal cessAmount = Math.Round(taxable * (cessRate / 100.0m), 2);
                decimal itemTaxTotal = cgstAmount + sgstAmount + igstAmount + cessAmount;
                decimal lineTotal = taxable + itemTaxTotal;

                totalTaxable += taxable;
                totalCGST += cgstAmount;
                totalSGST += sgstAmount;
                totalIGST += igstAmount;
                totalCess += cessAmount;

                result.LineItems.Add(new TaxLineItemResultDto
                {
                    ServiceOrInventoryId = item.ServiceOrInventoryId,
                    ItemType = item.ItemType,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TaxableValue = taxable,
                    HSNCode = hsn,
                    SACCode = sac,
                    TaxRate = gstRate,
                    CGSTRate = cgstRate,
                    CGSTAmount = cgstAmount,
                    SGSTRate = sgstRate,
                    SGSTAmount = sgstAmount,
                    IGSTRate = igstRate,
                    IGSTAmount = igstAmount,
                    CessRate = cessRate,
                    CessAmount = cessAmount,
                    LineTotal = lineTotal
                });
            }

            result.TaxableAmount = Math.Round(totalTaxable, 2);
            result.TotalCGST = Math.Round(totalCGST, 2);
            result.TotalSGST = Math.Round(totalSGST, 2);
            result.TotalIGST = Math.Round(totalIGST, 2);
            result.TotalCess = Math.Round(totalCess, 2);
            result.TotalTaxAmount = Math.Round(totalCGST + totalSGST + totalIGST + totalCess, 2);
            result.GrandTotal = Math.Round(result.TaxableAmount + result.TotalTaxAmount, 2);

            return result;
        }
    }
}
