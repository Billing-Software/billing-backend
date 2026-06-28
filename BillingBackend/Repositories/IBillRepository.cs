using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.Data.Entities;

namespace BillingBackend.Repositories
{
    public interface IBillRepository
    {
        Task<Bill?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<Bill>> GetByBusinessIdAsync(
            int businessId,
            int? customerId = null,
            int? staffId = null,
            int? branchId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null);
        Task<Bill> AddAsync(Bill bill, string itemsJson);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
