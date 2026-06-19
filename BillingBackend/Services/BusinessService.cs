using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly IBusinessRepository _businessRepository;

        public BusinessService(IBusinessRepository businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<BusinessDto?> GetByIdAsync(int id)
        {
            var business = await _businessRepository.GetByIdAsync(id);
            if (business == null) return null;

            return MapToDto(business);
        }

        public async Task<BusinessDto> UpdateAsync(BusinessDto dto)
        {
            var business = new Business
            {
                Id = dto.Id,
                OwnerId = dto.OwnerId,
                LegalName = dto.LegalName,
                TradingName = dto.TradingName,
                LogoUrl = dto.LogoUrl,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country ?? "India",
                Phone = dto.Phone,
                Email = dto.Email,
                Website = dto.Website,
                GstIn = dto.GstIn,
                DefaultTaxRate = dto.DefaultTaxRate,
                PricesIncludeTax = dto.PricesIncludeTax
            };

            var updated = await _businessRepository.UpdateAsync(business);
            return MapToDto(updated);
        }

        private BusinessDto MapToDto(Business b)
        {
            return new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                LegalName = b.LegalName,
                TradingName = b.TradingName,
                LogoUrl = b.LogoUrl,
                Address = b.Address,
                City = b.City,
                State = b.State,
                PostalCode = b.PostalCode,
                Country = b.Country,
                Phone = b.Phone,
                Email = b.Email,
                Website = b.Website,
                GstIn = b.GstIn,
                DefaultTaxRate = b.DefaultTaxRate,
                PricesIncludeTax = b.PricesIncludeTax
            };
        }
    }
}
