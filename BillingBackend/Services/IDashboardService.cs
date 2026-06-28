using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IDashboardService
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int businessId);
        Task<DashboardDataDto> GetStaffDashboardDataAsync(int businessId, int userId);
    }
}
