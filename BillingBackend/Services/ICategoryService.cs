using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetByBusinessIdAsync(int businessId);
        Task<CategoryDto> AddAsync(int businessId, CategoryDto dto);
        Task<CategoryDto?> UpdateAsync(int businessId, int id, CategoryDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
