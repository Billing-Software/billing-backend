using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IDashboardService
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int businessId);
    }
}
