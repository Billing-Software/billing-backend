using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface IPurchaseService
    {
        Task<IEnumerable<PurchaseDto>> GetByBusinessIdAsync(int businessId);
        Task<PurchaseDto?> GetByIdAsync(int businessId, int id);
        Task<PurchaseDto> AddAsync(int businessId, PurchaseDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
