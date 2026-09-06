using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services.Sms
{
    public class ExotelSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly BillingDbContext _db;
        private readonly ILogger<ExotelSmsService> _logger;

        private readonly string _apiKey;
        private readonly string _apiToken;
        private readonly string _accountSid;
        private readonly string _baseUrl;

        public ExotelSmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            BillingDbContext db,
            ILogger<ExotelSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _db = db;
            _logger = logger;

            _apiKey = _configuration["Exotel:ApiKey"] ?? string.Empty;
            _apiToken = _configuration["Exotel:ApiToken"] ?? string.Empty;
            _accountSid = _configuration["Exotel:AccountSid"] ?? string.Empty;
            _baseUrl = (_configuration["Exotel:BaseUrl"] ?? "https://api.in.exotel.com").TrimEnd('/');
        }

        public async Task<BusinessSmsSettingsDto?> GetSettingsAsync(int businessId)
        {
            var settings = await _db.BusinessSmsSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusinessId == businessId);

            if (settings == null) return null;

            return MapToDto(settings);
        }

        public async Task<BusinessSmsSettingsDto> SaveSettingsAsync(int businessId, UpdateSmsSettingsDto dto)
        {
            var cleanSenderId = dto.SenderId.Trim().ToUpperInvariant();

            var settings = await _db.BusinessSmsSettings
                .FirstOrDefaultAsync(x => x.BusinessId == businessId);

            if (settings == null)
            {
                settings = new BusinessSmsSettings
                {
                    BusinessId = businessId,
                    Provider = "Exotel",
                    CreatedAt = DateTime.UtcNow
                };
                _db.BusinessSmsSettings.Add(settings);
            }

            settings.SenderId = cleanSenderId;
            settings.DltEntityId = dto.DltEntityId.Trim();
            settings.InvoiceTemplateId = dto.InvoiceTemplateId.Trim();
            settings.TemplateBody = !string.IsNullOrWhiteSpace(dto.TemplateBody) ? dto.TemplateBody.Trim() : null;
            settings.IsActive = dto.IsActive;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("[ExotelSmsService] Saved SMS settings for Business {BusinessId}: SenderId={SenderId}, DltEntityId={DltEntityId}",
                businessId, cleanSenderId, settings.DltEntityId);

            return MapToDto(settings);
        }

        public async Task<SmsResult> SendAsync(int businessId, string phoneNumber, string message, string? templateId = null)
        {
            return await SendInternalAsync(businessId, phoneNumber, message, templateId, billId: null);
        }

        public async Task<SmsResult> SendInvoiceSmsAsync(int businessId, int billId, string customerPhone)
        {
            var bill = await _db.Bills
                .Include(b => b.Customer)
                .Include(b => b.Business)
                .FirstOrDefaultAsync(b => b.Id == billId && b.BusinessId == businessId);

            if (bill == null)
            {
                return new SmsResult { Success = false, Error = $"Bill #{billId} not found for business." };
            }

            var settings = await _db.BusinessSmsSettings
                .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.IsActive);

            if (settings == null)
            {
                return new SmsResult
                {
                    Success = false,
                    Error = "SMS is not configured or is inactive for this business. Please configure Sender ID and DLT Template ID in Settings -> SMS."
                };
            }

            var customerName = bill.Customer?.Name ?? "Customer";
            var billNumber = bill.BillNumber ?? $"INV-{bill.Id}";
            var storeName = bill.Business?.LegalName ?? "BillCom Store";
            var totalAmount = bill.TotalAmount.ToString("F2");
            var pdfUrl = !string.IsNullOrEmpty(bill.InvoicePdfUrl) ? bill.InvoicePdfUrl : $"https://billcom.in/i/{bill.Id}";

            // Construct message body matching the DLT-registered template format:
            // "Dear {#var#}, your invoice {#var#} from {#var#} for Rs.{#var#} is ready. View: {#var#}"
            string messageBody;
            if (!string.IsNullOrWhiteSpace(settings.TemplateBody))
            {
                messageBody = settings.TemplateBody
                    .Replace("{{1}}", customerName)
                    .Replace("{{2}}", billNumber)
                    .Replace("{{3}}", storeName)
                    .Replace("{{4}}", totalAmount)
                    .Replace("{{5}}", pdfUrl);
            }
            else
            {
                messageBody = $"Dear {customerName}, your invoice {billNumber} from {storeName} for Rs.{totalAmount} is ready. View: {pdfUrl}";
            }

            var effectivePhone = !string.IsNullOrWhiteSpace(customerPhone)
                ? customerPhone
                : (bill.Customer?.Phone ?? string.Empty);

            return await SendInternalAsync(businessId, effectivePhone, messageBody, settings.InvoiceTemplateId, billId);
        }

        public async Task<IEnumerable<SmsLogDto>> GetLogsAsync(int businessId, int? billId = null)
        {
            var query = _db.SmsLogs
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId);

            if (billId.HasValue)
            {
                query = query.Where(x => x.BillId == billId.Value);
            }

            var logs = await query
                .OrderByDescending(x => x.SentAt)
                .Take(100)
                .Select(x => new SmsLogDto
                {
                    Id = x.Id,
                    BillId = x.BillId,
                    RecipientPhone = x.RecipientPhone,
                    SenderId = x.SenderId,
                    MessageBody = x.MessageBody,
                    DltEntityId = x.DltEntityId,
                    DltTemplateId = x.DltTemplateId,
                    ExotelSid = x.ExotelSid,
                    Status = x.Status,
                    ErrorMessage = x.ErrorMessage,
                    SentAt = x.SentAt
                })
                .ToListAsync();

            return logs;
        }

        private async Task<SmsResult> SendInternalAsync(
            int businessId,
            string phoneNumber,
            string message,
            string? templateId,
            int? billId)
        {
            var settings = await _db.BusinessSmsSettings
                .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.IsActive);

            if (settings == null)
            {
                return new SmsResult
                {
                    Success = false,
                    Error = "SMS is not configured or is inactive for this business."
                };
            }

            var formattedPhone = FormatPhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(formattedPhone))
            {
                return new SmsResult { Success = false, Error = "Invalid recipient phone number." };
            }

            var effectiveTemplateId = !string.IsNullOrEmpty(templateId)
                ? templateId
                : settings.InvoiceTemplateId;

            _logger.LogInformation("==================================================");
            _logger.LogInformation("[ExotelSmsService] Dispatching SMS via Exotel");
            _logger.LogInformation("BusinessId: {BusinessId} | From: {From} | To: {To} | DLT Entity: {Entity} | DLT Template: {Template}",
                businessId, settings.SenderId, formattedPhone, settings.DltEntityId, effectiveTemplateId);
            _logger.LogInformation("Message Body: {Message}", message);
            _logger.LogInformation("==================================================");

            // Check if global Exotel credentials are mock/placeholder in dev environment
            if (string.IsNullOrEmpty(_apiKey) ||
                _apiKey.StartsWith("YOUR_") ||
                string.IsNullOrEmpty(_accountSid) ||
                _accountSid.StartsWith("YOUR_"))
            {
                var simulatedSid = $"SM_simulated_{Guid.NewGuid():N}";
                _logger.LogInformation("[ExotelSmsService] Development mode detected (placeholder Exotel credentials). Simulated dispatch ID: {Sid}", simulatedSid);

                var simLog = new SmsLog
                {
                    BusinessId = businessId,
                    BillId = billId,
                    RecipientPhone = formattedPhone,
                    SenderId = settings.SenderId,
                    MessageBody = message,
                    DltEntityId = settings.DltEntityId,
                    DltTemplateId = effectiveTemplateId,
                    ExotelSid = simulatedSid,
                    Status = "Sent",
                    SentAt = DateTime.UtcNow
                };

                _db.SmsLogs.Add(simLog);
                await _db.SaveChangesAsync();

                return new SmsResult
                {
                    Success = true,
                    MessageId = simulatedSid
                };
            }

            try
            {
                var url = $"{_baseUrl}/v1/Accounts/{_accountSid}/Sms/send";

                var formValues = new Dictionary<string, string>
                {
                    { "From", settings.SenderId },
                    { "To", formattedPhone },
                    { "Body", message },
                    { "DltEntityId", settings.DltEntityId },
                    { "DltTemplateId", effectiveTemplateId }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(formValues)
                };

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:{_apiToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[ExotelSmsService] Exotel API returned HTTP {StatusCode}: {Body}",
                        response.StatusCode, responseBody);

                    var errLog = new SmsLog
                    {
                        BusinessId = businessId,
                        BillId = billId,
                        RecipientPhone = formattedPhone,
                        SenderId = settings.SenderId,
                        MessageBody = message,
                        DltEntityId = settings.DltEntityId,
                        DltTemplateId = effectiveTemplateId,
                        Status = "Failed",
                        ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}",
                        SentAt = DateTime.UtcNow
                    };
                    _db.SmsLogs.Add(errLog);
                    await _db.SaveChangesAsync();

                    return new SmsResult
                    {
                        Success = false,
                        Error = $"Exotel Error ({(int)response.StatusCode}): {responseBody}"
                    };
                }

                var messageId = ExtractMessageId(responseBody);

                var successLog = new SmsLog
                {
                    BusinessId = businessId,
                    BillId = billId,
                    RecipientPhone = formattedPhone,
                    SenderId = settings.SenderId,
                    MessageBody = message,
                    DltEntityId = settings.DltEntityId,
                    DltTemplateId = effectiveTemplateId,
                    ExotelSid = messageId,
                    Status = "Sent",
                    SentAt = DateTime.UtcNow
                };
                _db.SmsLogs.Add(successLog);
                await _db.SaveChangesAsync();

                _logger.LogInformation("[ExotelSmsService] SMS dispatched successfully. Exotel Sid: {Sid}", messageId);

                return new SmsResult
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExotelSmsService] Exception dispatching SMS via Exotel");

                var excLog = new SmsLog
                {
                    BusinessId = businessId,
                    BillId = billId,
                    RecipientPhone = formattedPhone,
                    SenderId = settings.SenderId,
                    MessageBody = message,
                    DltEntityId = settings.DltEntityId,
                    DltTemplateId = effectiveTemplateId,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    SentAt = DateTime.UtcNow
                };
                _db.SmsLogs.Add(excLog);
                await _db.SaveChangesAsync();

                return new SmsResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private static string? ExtractMessageId(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return null;

            // Try JSON parsing first
            try
            {
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("SMSMessage", out var msgProp) &&
                    msgProp.TryGetProperty("Sid", out var sidProp))
                {
                    return sidProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("sid", out var directSid))
                {
                    return directSid.GetString();
                }
            }
            catch
            {
                // Not JSON, try XML
            }

            // Try XML parsing (Exotel default response format)
            try
            {
                var xdoc = XDocument.Parse(response);
                var sidElement = xdoc.Descendants("Sid").FirstOrDefault();
                if (sidElement != null) return sidElement.Value;
            }
            catch
            {
                // Regex fallback
                var match = Regex.Match(response, @"<Sid>(.*?)<\/Sid>");
                if (match.Success) return match.Groups[1].Value;
            }

            return null;
        }

        private static string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
            {
                // Format Indian mobile number as 91XXXXXXXXXX or standard 10-digit
                return digits;
            }
            if (digits.Length == 12 && digits.StartsWith("91"))
            {
                return digits;
            }
            return digits;
        }

        private static BusinessSmsSettingsDto MapToDto(BusinessSmsSettings s)
        {
            return new BusinessSmsSettingsDto
            {
                Id = s.Id,
                BusinessId = s.BusinessId,
                Provider = s.Provider,
                SenderId = s.SenderId,
                DltEntityId = s.DltEntityId,
                InvoiceTemplateId = s.InvoiceTemplateId,
                TemplateBody = s.TemplateBody,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            };
        }
    }
}
