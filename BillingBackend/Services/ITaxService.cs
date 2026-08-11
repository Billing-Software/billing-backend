using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface ITaxService
    {
        Task<IEnumerable<TaxCategoryDto>> GetTaxCategoriesAsync(int businessId);
        Task<TaxCategoryDto> CreateTaxCategoryAsync(int businessId, TaxCategoryDto dto);
        Task<IEnumerable<HSNMaster>> SearchHSNAsync(string query);
        Task<IEnumerable<SACMaster>> SearchSACAsync(string query);
        Task<TaxCalculationResultDto> CalculateTaxAsync(int businessId, TaxCalculationRequestDto request);
    }
}
