using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class BillService : IBillService
    {
        private readonly IBillRepository _billRepository;
        private readonly IAuditService _auditService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<BillService> _logger;

        // Valid state transitions for the payment state machine
        private static readonly Dictionary<string, List<string>> ValidTransitions = new()
        {
            { "Pending", new List<string> { "Paid", "Failed", "Cancelled" } },
            { "Failed", new List<string> { "Pending", "Paid" } },      // Allow retry
            { "Paid", new List<string> { "Refunded", "PartialRefund" } },
            { "Cancelled", new List<string>() },                        // Terminal state
            { "Refunded", new List<string>() },                         // Terminal state
            { "PartialRefund", new List<string> { "Refunded" } },
        };

        public BillService(
            IBillRepository billRepository,
            IAuditService auditService,
            ISettingsService settingsService,
            ILogger<BillService> logger)
        {
            _billRepository = billRepository;
            _auditService = auditService;
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task<BillDto?> GetByIdAsync(int businessId, int id)
        {
            try
            {
                var bill = await _billRepository.GetByIdAsync(businessId, id);
                return bill == null ? null : MapToDto(bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to get bill {BillId} for business {BusinessId}", id, businessId);
                throw;
            }
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
            try
            {
                var bills = await _billRepository.GetByBusinessIdAsync(
                    businessId, customerId, staffId, branchId, startDate, endDate, status, minAmount, maxAmount);
                return bills.Select(b => MapToDto(b));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to list bills for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<BillDto> AddAsync(int businessId, CreateBillDto dto)
        {
            try
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
                    Status = dto.Status,
                    IdempotencyKey = dto.IdempotencyKey,
                    PaymentReference = dto.PaymentReference,
                    Notes = dto.Notes
                };

                // Serialize items list to JSON for stored procedure OPENJSON parsing
                var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var itemsJson = JsonSerializer.Serialize(dto.Items, serializerOptions);

                var added = await _billRepository.AddAsync(bill, itemsJson);
                
                // Reload the complete bill including details and items
                var reloaded = await _billRepository.GetByIdAsync(businessId, added.Id);
                var result = reloaded != null ? MapToDto(reloaded) : MapToDto(added);

                // Audit trail
                await _auditService.LogAsync(
                    businessId, "Bill", result.Id, "Created",
                    newValues: new { result.BillNumber, result.TotalAmount, result.PaymentMethod, result.Status, result.IdempotencyKey },
                    description: $"Bill {result.BillNumber} created for ₹{result.TotalAmount}");

                _logger.LogInformation(
                    "[BillService] Bill {BillNumber} created successfully | BillId: {BillId} | Amount: {Amount} | Business: {BusinessId}",
                    result.BillNumber, result.Id, result.TotalAmount, businessId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to create bill for business {BusinessId} | IdempotencyKey: {Key}",
                    businessId, dto.IdempotencyKey);
                throw;
            }
        }

        public async Task<BillDto?> UpdateStatusAsync(
            int businessId, 
            int billId, 
            string newStatus, 
            string? paymentMethod = null, 
            string? paymentReference = null, 
            string? notes = null)
        {
            try
            {
                var bill = await _billRepository.GetByIdAsync(businessId, billId);
                if (bill == null)
                {
                    _logger.LogWarning("[BillService] Bill {BillId} not found for status update in business {BusinessId}", billId, businessId);
                    return null;
                }

                var oldStatus = bill.Status;

                // Validate state transition if status is actually changing
                if (oldStatus != newStatus)
                {
                    if (!ValidTransitions.ContainsKey(oldStatus) || !ValidTransitions[oldStatus].Contains(newStatus))
                    {
                        _logger.LogWarning(
                            "[BillService] Invalid state transition: {OldStatus} → {NewStatus} for bill {BillId}",
                            oldStatus, newStatus, billId);
                        throw new InvalidOperationException($"Invalid status transition: {oldStatus} → {newStatus}");
                    }
                }

                bill.Status = newStatus;
                bill.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(paymentMethod)) bill.PaymentMethod = paymentMethod.Trim();
                if (paymentReference != null) bill.PaymentReference = paymentReference.Trim();
                if (notes != null) bill.Notes = notes.Trim();

                await _billRepository.UpdateAsync(bill);

                // Audit trail
                await _auditService.LogAsync(
                    businessId, "Bill", billId, "StatusChanged",
                    oldValues: new { Status = oldStatus },
                    newValues: new { Status = newStatus, PaymentMethod = bill.PaymentMethod, PaymentReference = bill.PaymentReference },
                    description: $"Bill {bill.BillNumber} status changed: {oldStatus} → {newStatus}");

                _logger.LogInformation(
                    "[BillService] Bill {BillId} status updated: {OldStatus} → {NewStatus} | Business: {BusinessId}",
                    billId, oldStatus, newStatus, businessId);

                return MapToDto(bill);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to update status for bill {BillId}", billId);
                throw;
            }
        }

        public async Task<BillDto?> RecordPaymentAsync(int businessId, int billId, RecordBillPaymentDto dto)
        {
            try
            {
                var bill = await _billRepository.GetByIdAsync(businessId, billId);
                if (bill == null)
                {
                    _logger.LogWarning("[BillService] Bill {BillId} not found for payment recording in business {BusinessId}", billId, businessId);
                    return null;
                }

                var oldStatus = bill.Status;
                var oldMethod = bill.PaymentMethod;
                var newStatus = string.IsNullOrWhiteSpace(dto.Status) ? "Paid" : dto.Status.Trim();

                // Validate state transition if status is actually changing
                if (oldStatus != newStatus)
                {
                    if (!ValidTransitions.ContainsKey(oldStatus) || !ValidTransitions[oldStatus].Contains(newStatus))
                    {
                        _logger.LogWarning(
                            "[BillService] Invalid state transition: {OldStatus} → {NewStatus} for bill {BillId}",
                            oldStatus, newStatus, billId);
                        throw new InvalidOperationException($"Invalid status transition: {oldStatus} → {newStatus}");
                    }
                }

                bill.Status = newStatus;
                bill.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod.Trim();
                if (!string.IsNullOrWhiteSpace(dto.PaymentReference))
                {
                    bill.PaymentReference = dto.PaymentReference.Trim();
                }
                if (!string.IsNullOrWhiteSpace(dto.Notes))
                {
                    bill.Notes = dto.Notes.Trim();
                }
                bill.UpdatedAt = DateTime.UtcNow;

                await _billRepository.UpdateAsync(bill);

                await _auditService.LogAsync(
                    businessId, "Bill", billId, "PaymentRecorded",
                    oldValues: new { Status = oldStatus, PaymentMethod = oldMethod },
                    newValues: new { Status = newStatus, PaymentMethod = bill.PaymentMethod, PaymentReference = bill.PaymentReference },
                    description: $"Bill {bill.BillNumber} payment recorded: ₹{bill.TotalAmount} via {bill.PaymentMethod}");

                _logger.LogInformation(
                    "[BillService] Bill {BillId} payment recorded: {Status} via {PaymentMethod} (Ref: {PaymentReference}) | Business: {BusinessId}",
                    billId, bill.Status, bill.PaymentMethod, bill.PaymentReference, businessId);

                return MapToDto(bill);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to record payment for bill {BillId}", billId);
                throw;
            }
        }

        public async Task<BillUpiQrResponseDto?> GenerateUpiQrAsync(int businessId, int billId)
        {
            try
            {
                var bill = await _billRepository.GetByIdAsync(businessId, billId);
                if (bill == null)
                {
                    _logger.LogWarning("[BillService] Bill {BillId} not found for UPI QR generation in business {BusinessId}", billId, businessId);
                    return null;
                }

                var paymentSettings = await _settingsService.GetPaymentSettingsAsync(businessId);
                var upiVpa = paymentSettings?.UpiVpa?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(upiVpa))
                {
                    upiVpa = "merchant@upi";
                }

                var payeeName = paymentSettings?.AccountHolderName?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(payeeName))
                {
                    payeeName = bill.Branch?.Name ?? "Merchant";
                }

                var formattedAmount = bill.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                var note = $"Bill {bill.BillNumber}";
                var encodedPayee = Uri.EscapeDataString(payeeName);
                var encodedNote = Uri.EscapeDataString(note);
                var encodedRef = Uri.EscapeDataString(bill.BillNumber);

                // NPCI Standard Deep Link
                var upiUri = $"upi://pay?pa={upiVpa}&pn={encodedPayee}&am={formattedAmount}&cu=INR&tn={encodedNote}&tr={encodedRef}";

                return new BillUpiQrResponseDto
                {
                    BillId = bill.Id,
                    BillNumber = bill.BillNumber,
                    Amount = bill.TotalAmount,
                    UpiVpa = upiVpa,
                    PayeeName = payeeName,
                    TransactionNote = note,
                    UpiUri = upiUri,
                    Status = bill.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to generate dynamic UPI QR for bill {BillId}", billId);
                throw;
            }
        }

        public async Task<BillDto?> GetByIdempotencyKeyAsync(int businessId, string idempotencyKey)
        {
            try
            {
                var bill = await _billRepository.GetByIdempotencyKeyAsync(businessId, idempotencyKey);
                return bill == null ? null : MapToDto(bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to look up bill by idempotency key {Key}", idempotencyKey);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int businessId, int id)
        {
            try
            {
                var bill = await _billRepository.GetByIdAsync(businessId, id);
                if (bill == null) return false;

                var result = await _billRepository.DeleteAsync(businessId, id);

                if (result)
                {
                    await _auditService.LogAsync(
                        businessId, "Bill", id, "Deleted",
                        oldValues: new { bill.BillNumber, bill.TotalAmount, bill.Status, bill.PaymentMethod },
                        description: $"Bill {bill.BillNumber} (₹{bill.TotalAmount}) deleted");

                    _logger.LogInformation(
                        "[BillService] Bill {BillNumber} deleted | BillId: {BillId} | Business: {BusinessId}",
                        bill.BillNumber, id, businessId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BillService] Failed to delete bill {BillId} for business {BusinessId}", id, businessId);
                throw;
            }
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
                IdempotencyKey = b.IdempotencyKey,
                PaymentReference = b.PaymentReference,
                Notes = b.Notes,
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
                    ItemType = bi.ItemType ?? "Service",
                    UnitPrice = bi.UnitPrice,
                    Quantity = bi.Quantity,
                    LineTotal = bi.LineTotal
                }).ToList()
            };
        }
    }
}
