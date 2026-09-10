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
        private readonly IBranchRepository _branchRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IStaffRepository _staffRepository;
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
            IBranchRepository branchRepository,
            ICustomerRepository customerRepository,
            IStaffRepository staffRepository,
            IAuditService auditService,
            ISettingsService settingsService,
            ILogger<BillService> logger)
        {
            _billRepository = billRepository;
            _branchRepository = branchRepository;
            _customerRepository = customerRepository;
            _staffRepository = staffRepository;
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
                if (businessId <= 0)
                    throw new InvalidOperationException("Invalid business scope.");
                if (dto.Items == null || dto.Items.Count == 0)
                    throw new InvalidOperationException("At least one bill item is required.");
                if (dto.Items.Count > 200)
                    throw new InvalidOperationException("Too many bill items.");

                // IDOR guards: every referenced entity must belong to this business.
                var branch = await _branchRepository.GetByIdAsync(businessId, dto.BranchId);
                if (branch == null)
                    throw new InvalidOperationException("Selected branch does not belong to this business.");
                var customer = await _customerRepository.GetByIdAsync(businessId, dto.CustomerId);
                if (customer == null)
                    throw new InvalidOperationException("Selected customer does not belong to this business.");
                int? staffId = null;
                if (dto.CreatedByStaffId.HasValue)
                {
                    var staff = await _staffRepository.GetByIdAsync(businessId, dto.CreatedByStaffId.Value);
                    if (staff == null)
                        throw new InvalidOperationException("Selected staff does not belong to this business.");
                    staffId = staff.Id;
                }

                // Server-side totals: never trust client Subtotal/Discount/Tax/Total.
                decimal subtotal = 0;
                foreach (var it in dto.Items)
                {
                    if (it.Quantity <= 0 || it.Quantity > 10000)
                        throw new InvalidOperationException("Invalid item quantity.");
                    if (it.UnitPrice < 0 || it.UnitPrice > 10_000_000)
                        throw new InvalidOperationException("Invalid item price.");
                    if (string.IsNullOrWhiteSpace(it.ServiceName))
                        throw new InvalidOperationException("Item name is required.");
                    subtotal += Math.Round(it.UnitPrice * it.Quantity, 2, MidpointRounding.AwayFromZero);
                }
                subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
                var discount = Math.Round(dto.DiscountAmount, 2, MidpointRounding.AwayFromZero);
                var tax = Math.Round(dto.TaxAmount, 2, MidpointRounding.AwayFromZero);
                if (discount < 0 || discount > subtotal)
                    throw new InvalidOperationException("Invalid discount amount.");
                if (tax < 0 || tax > subtotal * 2)
                    throw new InvalidOperationException("Invalid tax amount.");
                var total = Math.Round(subtotal - discount + tax, 2, MidpointRounding.AwayFromZero);
                if (total < 0)
                    throw new InvalidOperationException("Invalid bill total.");

                var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "Cash", "UPI", "Card", "NetBanking", "Wallet", "Razorpay" };
                var paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim();
                if (!allowedMethods.Contains(paymentMethod))
                    throw new InvalidOperationException("Invalid payment method.");
                var allowedStatus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "Pending", "Paid", "Failed", "Cancelled" };
                var status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending" : dto.Status.Trim();
                if (!allowedStatus.Contains(status))
                    throw new InvalidOperationException("Invalid bill status.");

                var bill = new Bill
                {
                    BusinessId = businessId,
                    BranchId = branch.Id,
                    CustomerId = customer.Id,
                    CreatedByStaffId = staffId,
                    BillNumber = dto.BillNumber.Trim(),
                    Subtotal = subtotal,
                    DiscountCode = string.IsNullOrWhiteSpace(dto.DiscountCode) ? null : dto.DiscountCode.Trim(),
                    DiscountAmount = discount,
                    TaxAmount = tax,
                    TotalAmount = total,
                    PaymentMethod = paymentMethod,
                    Status = status,
                    IdempotencyKey = string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? null : dto.IdempotencyKey.Trim(),
                    PaymentReference = string.IsNullOrWhiteSpace(dto.PaymentReference) ? null : dto.PaymentReference.Trim(),
                    Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
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
