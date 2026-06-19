using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IBusinessService
    {
        Task<BusinessDto?> GetByIdAsync(int id);
        Task<BusinessDto> UpdateAsync(BusinessDto dto);
    }
}
