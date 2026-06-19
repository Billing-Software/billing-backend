using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(int businessId)
        {
            return await _dashboardRepository.GetDashboardDataAsync(businessId);
        }
    }
}
