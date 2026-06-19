using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int businessId);
    }
}
