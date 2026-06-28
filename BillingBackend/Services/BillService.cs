using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class BillService : IBillService
    {
        private readonly IBillRepository _billRepository;

        public BillService(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<BillDto?> GetByIdAsync(int businessId, int id)
        {
            var bill = await _billRepository.GetByIdAsync(businessId, id);
            return bill == null ? null : MapToDto(bill);
        }

        public async Task<IEnumerable<BillDto>> GetByBusinessIdAsync(
            int businessId,
            int? customerId = null,
            int? staffId = null,
            int? branchId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null)
        {
            var bills = await _billRepository.GetByBusinessIdAsync(
                businessId, customerId, staffId, branchId, startDate, endDate, status, minAmount, maxAmount);
            return bills.Select(b => MapToDto(b));
        }

        public async Task<BillDto> AddAsync(int businessId, CreateBillDto dto)
        {
            var bill = new Bill
            {
                BusinessId = businessId,
                BranchId = dto.BranchId,
                CustomerId = dto.CustomerId,
                CreatedByStaffId = dto.CreatedByStaffId,
                BillNumber = dto.BillNumber,
                Subtotal = dto.Subtotal,
                DiscountCode = dto.DiscountCode,
                DiscountAmount = dto.DiscountAmount,
                TaxAmount = dto.TaxAmount,
                TotalAmount = dto.TotalAmount,
                PaymentMethod = dto.PaymentMethod,
                Status = dto.Status
            };

            // Serialize items list to JSON for stored procedure OPENJSON parsing
            var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var itemsJson = JsonSerializer.Serialize(dto.Items, serializerOptions);

            var added = await _billRepository.AddAsync(bill, itemsJson);
            
            // Reload the complete bill including details and items
            var reloaded = await _billRepository.GetByIdAsync(businessId, added.Id);
            return reloaded != null ? MapToDto(reloaded) : MapToDto(added);
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            return await _billRepository.DeleteAsync(businessId, id);
        }

        private BillDto MapToDto(Bill b)
        {
            return new BillDto
            {
                Id = b.Id,
                BusinessId = b.BusinessId,
                BranchId = b.BranchId,
                CustomerId = b.CustomerId,
                CreatedByStaffId = b.CreatedByStaffId,
                BillNumber = b.BillNumber,
                Subtotal = b.Subtotal,
                DiscountCode = b.DiscountCode,
                DiscountAmount = b.DiscountAmount,
                TaxAmount = b.TaxAmount,
                TotalAmount = b.TotalAmount,
                PaymentMethod = b.PaymentMethod,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                CustomerName = b.Customer?.Name,
                CustomerPhone = b.Customer?.Phone,
                CustomerEmail = b.Customer?.Email,
                StaffName = b.CreatedByStaff?.Name,
                BranchName = b.Branch?.Name,
                Items = b.Items.Select(bi => new BillItemDto
                {
                    Id = bi.Id,
                    BillId = bi.BillId,
                    ServiceId = bi.ServiceId,
                    ServiceName = bi.ServiceName,
                    UnitPrice = bi.UnitPrice,
                    Quantity = bi.Quantity,
                    LineTotal = bi.LineTotal
                }).ToList()
            };
        }
    }
}
