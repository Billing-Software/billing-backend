using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IBillService
    {
        Task<BillDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<BillDto>> GetByBusinessIdAsync(int businessId);
        Task<BillDto> AddAsync(int businessId, CreateBillDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
