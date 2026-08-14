using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface IBusinessConfigurationService
    {
        Task<List<BusinessTypePresetDto>> GetBusinessTypePresetsAsync();
        Task<BusinessTypePresetDto> GetPresetByTypeNameAsync(string typeName);
        Task<BusinessConfigDto> GetConfigurationAsync(int businessId);
        Task<BusinessConfigDto> GetConfigurationAsync(int businessId, string userRole);
        Task<BusinessConfigDto> UpdateConfigurationAsync(int businessId, UpdateBusinessConfigDto dto);
    }
}
