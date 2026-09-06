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
            try
            {
                var business = await _businessRepository.GetByIdAsync(id);
                if (business == null) return null;

                return MapToDto(business);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BusinessService.GetByIdAsync Error]: {ex.Message}");
                throw;
            }
        }

        public async Task<BusinessDto> UpdateAsync(BusinessDto dto)
        {
            try
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
                    GstIn = string.IsNullOrWhiteSpace(dto.GstIn) ? null : dto.GstIn.Trim(),
                    GstScheme = string.IsNullOrEmpty(dto.GstScheme)
                        ? (string.IsNullOrWhiteSpace(dto.GstIn) ? "None" : "Regular")
                        : dto.GstScheme,
                    DefaultTaxRate = (dto.GstScheme == "None" || dto.GstScheme == "Non-GST" || string.IsNullOrWhiteSpace(dto.GstIn))
                        ? 0.0m
                        : dto.DefaultTaxRate,
                    PricesIncludeTax = dto.PricesIncludeTax,
                    ReceiptHeader = dto.ReceiptHeader,
                    ReceiptFooter = dto.ReceiptFooter,
                    ShowLogoOnReceipt = dto.ShowLogoOnReceipt,
                    ReceiptTemplateType = dto.ReceiptTemplateType ?? "Thermal80mm"
                };

                var updated = await _businessRepository.UpdateAsync(business);
                return MapToDto(updated);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BusinessService.UpdateAsync Error]: {ex.Message}");
                throw;
            }
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
                GstScheme = b.GstScheme ?? "Regular",
                DefaultTaxRate = b.DefaultTaxRate,
                PricesIncludeTax = b.PricesIncludeTax,
                ReceiptHeader = b.ReceiptHeader,
                ReceiptFooter = b.ReceiptFooter,
                ShowLogoOnReceipt = b.ShowLogoOnReceipt,
                ReceiptTemplateType = b.ReceiptTemplateType,
                ActivePlanId = b.ActivePlanId,
                AllowedBranches = b.AllowedBranches,
                AllowedStaff = b.AllowedStaff,
                SubscriptionStatus = b.IsTrial
                    ? ((b.TrialEndsAt.HasValue && b.TrialEndsAt.Value < System.DateTime.UtcNow) ? "TrialExpired" : "Trial")
                    : b.SubscriptionStatus,
                SubscriptionExpiresAt = b.SubscriptionExpiresAt,
                IsTrial = b.IsTrial,
                TrialStartsAt = b.TrialStartsAt,
                TrialEndsAt = b.TrialEndsAt
            };
        }
    }
}
