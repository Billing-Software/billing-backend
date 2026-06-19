using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IInventoryService
    {
        Task<InventoryDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<InventoryDto>> GetByBusinessIdAsync(int businessId);
        Task<InventoryDto> AddAsync(int businessId, InventoryDto dto);
        Task<InventoryDto> UpdateAsync(int businessId, InventoryDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
