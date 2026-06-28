using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface IBillService
    {
        Task<BillDto?> GetByIdAsync(int businessId, int id);
        Task<IEnumerable<BillDto>> GetByBusinessIdAsync(
            int businessId,
            int? customerId = null,
            int? staffId = null,
            int? branchId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null);
        Task<BillDto> AddAsync(int businessId, CreateBillDto dto);
        Task<bool> DeleteAsync(int businessId, int id);
    }
}
