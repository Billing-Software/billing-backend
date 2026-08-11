using System;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    public interface IReportService
    {
        Task<GstReportSummaryDto> GetGstReportAsync(int businessId, int? month = null, int? year = null, DateTime? startDate = null, DateTime? endDate = null, int? branchId = null);
        Task<byte[]> ExportGstReportCsvAsync(int businessId, int? month = null, int? year = null, DateTime? startDate = null, DateTime? endDate = null, int? branchId = null);
    }
}
