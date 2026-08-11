using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IWhatsAppRepository _repository;
        private readonly IMetaApiClient _metaApiClient;
        private readonly ITokenEncryptionService _encryptionService;
        private readonly IBillRepository _billRepository;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            IWhatsAppRepository repository,
            IMetaApiClient metaApiClient,
            ITokenEncryptionService encryptionService,
            IBillRepository billRepository,
            ILogger<WhatsAppService> logger)
        {
            _repository = repository;
            _metaApiClient = metaApiClient;
            _encryptionService = encryptionService;
            _billRepository = billRepository;
            _logger = logger;
        }

        public async Task<WhatsAppAccountDto> ConnectAsync(int businessId, WhatsAppConnectCallbackDto dto)
        {
            _logger.LogInformation("Connecting WhatsApp for BusinessId: {BusinessId}", businessId);

            string accessToken = string.Empty;
            long? expiresIn = null;

            // Step 1: Handle direct access token or OAuth authorization code exchange
            if (!string.IsNullOrEmpty(dto.Code) && dto.Code.StartsWith("EAA"))
            {
                accessToken = dto.Code;
            }
            else if (!string.IsNullOrEmpty(dto.Code) && !dto.Code.StartsWith("MOCK_") && !dto.Code.StartsWith("META_TEST_"))
            {
                var tokenResponse = await _metaApiClient.ExchangeCodeForTokenAsync(dto.Code);
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    accessToken = tokenResponse.AccessToken;
                    expiresIn = tokenResponse.ExpiresIn;
                }
                else
                {
                    accessToken = dto.Code;
                }
            }
            else
            {
                accessToken = dto.Code;
            }

            // Step 2: Determine WABA ID, Phone Number ID, and Display Phone Number
            string? wabaId = dto.WabaId;
            string? phoneNumberId = dto.PhoneNumberId;
            string? displayPhoneNumber = dto.DisplayPhoneNumber;

            if (!string.IsNullOrEmpty(wabaId) && string.IsNullOrEmpty(phoneNumberId))
            {
                // Retrieve phone numbers under this WABA
                var phoneNumbers = await _metaApiClient.GetWabaPhoneNumbersAsync(wabaId, accessToken);
                if (phoneNumbers.Count > 0)
                {
                    phoneNumberId = phoneNumbers[0].Id;
                    displayPhoneNumber = phoneNumbers[0].DisplayPhoneNumber ?? displayPhoneNumber;
                }
            }

            if (string.IsNullOrEmpty(wabaId))
            {
                var bizInfo = await _metaApiClient.GetSharedWabaInfoAsync(accessToken);
                wabaId = bizInfo.WabaId ?? $"waba_{Guid.NewGuid():N}";
                phoneNumberId ??= bizInfo.PhoneNumberId ?? $"phone_id_{Guid.NewGuid():N}";
                displayPhoneNumber ??= bizInfo.DisplayPhoneNumber ?? "+91 98765 43210";
            }

            // Step 3: Subscribe BillCom App to the client's WABA webhooks
            if (!string.IsNullOrEmpty(wabaId) && !wabaId.StartsWith("waba_"))
            {
                await _metaApiClient.SubscribeWabaToAppAsync(wabaId, accessToken);
            }

            // Step 4: Create or update the WhatsApp account record
            var account = await _repository.GetByBusinessIdAsync(businessId);
            if (account == null)
            {
                account = new WhatsAppAccount
                {
                    BusinessId = businessId
                };
            }

            account.WabaId = wabaId;
            account.PhoneNumberId = phoneNumberId ?? $"phone_id_{Guid.NewGuid():N}";
            account.DisplayPhoneNumber = !string.IsNullOrEmpty(displayPhoneNumber) ? displayPhoneNumber : "+91 98765 43210";
            account.AccessToken = _encryptionService.Encrypt(accessToken);
            account.TokenExpiry = expiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(expiresIn.Value)
                : DateTime.UtcNow.AddDays(60);
            account.Status = "Connected";
            account.ConnectedAt = DateTime.UtcNow;
            account.DisconnectedAt = null;

            await _repository.CreateOrUpdateAsync(account);

            // Step 5: Automatically create / provision default BillCom invoice template on customer WABA
            try
            {
                await EnsureDefaultInvoiceTemplateAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-provision default invoice template for BusinessId {BusinessId}", businessId);
            }

            _logger.LogInformation("WhatsApp connected successfully for BusinessId: {BusinessId}, WABA: {WabaId}, PhoneId: {PhoneId}",
                businessId, account.WabaId, account.PhoneNumberId);

            return MapToDto(account);
        }

        public async Task<WhatsAppAccountDto?> GetStatusAsync(int businessId)
        {
            var account = await _repository.GetByBusinessIdAsync(businessId);
            return account != null ? MapToDto(account) : null;
        }

        public async Task<bool> DisconnectAsync(int businessId)
        {
            _logger.LogInformation("Disconnecting WhatsApp for BusinessId: {BusinessId}", businessId);
            return await _repository.DeleteByBusinessIdAsync(businessId);
        }

        public async Task<MessageLogDto> SendTextAsync(int businessId, SendTextMessageDto dto)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var decryptedToken = _encryptionService.Decrypt(account.AccessToken!);

            var cleanPhone = FormatWhatsAppPhone(dto.Phone);
            var result = await _metaApiClient.SendTextMessageAsync(
                account.PhoneNumberId!, decryptedToken, cleanPhone, dto.Message);

            var log = new MessageLog
            {
                WhatsAppAccountId = account.Id,
                RecipientPhone = dto.Phone,
                MessageType = "text",
                MetaMessageId = result.MessageId,
                Status = result.Success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow,
                FailedReason = result.Error
            };

            await _repository.AddMessageLogAsync(log);
            return MapLogToDto(log);
        }

        public async Task<MessageLogDto> SendDocumentAsync(int businessId, SendDocumentDto dto)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var decryptedToken = _encryptionService.Decrypt(account.AccessToken!);

            // Get the bill to find the PDF URL
            var bill = await _billRepository.GetByIdAsync(businessId, dto.BillId);
            if (bill == null)
                throw new InvalidOperationException($"Bill {dto.BillId} not found.");

            if (string.IsNullOrEmpty(bill.InvoicePdfUrl))
            {
                _logger.LogInformation("Bill {BillId} does not have a PDF URL yet. Falling back to WhatsApp Invoice Template message.", dto.BillId);
                return await SendInvoiceTemplateAsync(businessId, new SendInvoiceTemplateRequestDto
                {
                    BillId = dto.BillId,
                    Phone = dto.Phone
                });
            }

            var result = await _metaApiClient.SendDocumentMessageAsync(
                account.PhoneNumberId!, decryptedToken, dto.Phone,
                bill.InvoicePdfUrl, dto.Caption, $"Invoice_{bill.BillNumber}.pdf");

            var log = new MessageLog
            {
                WhatsAppAccountId = account.Id,
                BillId = dto.BillId,
                RecipientPhone = dto.Phone,
                MessageType = "document",
                MetaMessageId = result.MessageId,
                Status = result.Success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow,
                FailedReason = result.Error
            };

            await _repository.AddMessageLogAsync(log);
            return MapLogToDto(log);
        }

        public async Task<MessageLogDto> SendTemplateAsync(int businessId, SendTemplateDto dto)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var decryptedToken = _encryptionService.Decrypt(account.AccessToken!);

            var result = await _metaApiClient.SendTemplateMessageAsync(
                account.PhoneNumberId!, decryptedToken, dto.Phone, dto.TemplateName, dto.Parameters);

            var log = new MessageLog
            {
                WhatsAppAccountId = account.Id,
                RecipientPhone = dto.Phone,
                MessageType = "template",
                MetaMessageId = result.MessageId,
                Status = result.Success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow,
                FailedReason = result.Error
            };

            await _repository.AddMessageLogAsync(log);
            return MapLogToDto(log);
        }

        public async Task<IEnumerable<MessageLogDto>> GetMessageLogsAsync(int businessId, int? billId = null)
        {
            var logs = await _repository.GetMessageLogsByBusinessAsync(businessId, billId);
            return logs.Select(MapLogToDto);
        }

        public async Task<IEnumerable<WhatsAppTemplateDto>> GetTemplatesAsync(int businessId)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var templates = await _repository.GetTemplatesByAccountAsync(account.Id);

            return templates.Select(t => new WhatsAppTemplateDto
            {
                Id = t.Id,
                TemplateName = t.TemplateName,
                Language = t.Language,
                Category = t.Category,
                BodyText = t.BodyText,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            });
        }

        public async Task<WhatsAppTemplateDto> EnsureDefaultInvoiceTemplateAsync(int businessId)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var decryptedToken = _encryptionService.Decrypt(account.AccessToken!);

            const string templateName = "billcom_invoice_v1";
            const string category = "UTILITY";
            const string language = "en";
            const string bodyText = "Hello {{1}}, your invoice {{2}} for {{3}} has been generated by {{4}}.\n\nView and download your digital receipt:\n{{5}}\n\nThank you for your business!";

            string status = "APPROVED";

            // If WABA ID is valid, query/create with Meta Graph API
            if (!string.IsNullOrEmpty(account.WabaId) && !account.WabaId.StartsWith("waba_"))
            {
                var metaResp = await _metaApiClient.CreateWabaTemplateAsync(
                    account.WabaId, decryptedToken, templateName, category, language, bodyText);

                if (!string.IsNullOrEmpty(metaResp.Status))
                {
                    status = metaResp.Status;
                }
            }

            var tEntity = new WhatsAppTemplate
            {
                WhatsAppAccountId = account.Id,
                TemplateName = templateName,
                Language = language,
                Category = category,
                BodyText = bodyText,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _repository.SaveTemplateAsync(tEntity);

            _logger.LogInformation("Ensured WhatsApp invoice template '{TemplateName}' for BusinessId {BusinessId} with Status {Status}",
                templateName, businessId, saved.Status);

            return new WhatsAppTemplateDto
            {
                Id = saved.Id,
                TemplateName = saved.TemplateName,
                Language = saved.Language,
                Category = saved.Category,
                BodyText = saved.BodyText,
                Status = saved.Status,
                CreatedAt = saved.CreatedAt
            };
        }

        public async Task<MessageLogDto> SendInvoiceTemplateAsync(int businessId, SendInvoiceTemplateRequestDto dto)
        {
            var account = await GetConnectedAccountOrThrow(businessId);
            var decryptedToken = _encryptionService.Decrypt(account.AccessToken!);

            var bill = await _billRepository.GetByIdAsync(businessId, dto.BillId);
            if (bill == null)
                throw new InvalidOperationException($"Bill {dto.BillId} not found.");

            // Format parameters for billcom_invoice_v1 template
            var customerName = !string.IsNullOrEmpty(bill.Customer?.Name) ? bill.Customer.Name : "Customer";
            var billNumber = bill.BillNumber ?? $"INV-{bill.Id}";
            var totalAmount = $"₹{bill.TotalAmount:N2}";
            var pdfUrl = !string.IsNullOrEmpty(bill.InvoicePdfUrl) ? bill.InvoicePdfUrl : $"https://app.billcom.in/i/{bill.Id}";
            var businessName = "BillCom Merchant";

            string templateName = "billcom_invoice_v1";
            var parameters = new Dictionary<string, string>
            {
                { "1", customerName },
                { "2", billNumber },
                { "3", totalAmount },
                { "4", businessName },
                { "5", pdfUrl }
            };

            // Meta Sandbox Test Number uses pre-approved template 'jaspers_market_order_confirmation_v1'
            if (account.PhoneNumberId == "1273696479156949" || account.WabaId == "1063228732791303")
            {
                templateName = "jaspers_market_order_confirmation_v1";
                parameters = new Dictionary<string, string>
                {
                    { "1", customerName },
                    { "2", billNumber },
                    { "3", DateTime.UtcNow.ToString("MMM dd, yyyy") }
                };
            }

            var cleanPhone = FormatWhatsAppPhone(dto.Phone);
            var result = await _metaApiClient.SendTemplateMessageAsync(
                account.PhoneNumberId!, decryptedToken, cleanPhone, templateName, parameters);

            var log = new MessageLog
            {
                WhatsAppAccountId = account.Id,
                BillId = dto.BillId,
                RecipientPhone = dto.Phone,
                MessageType = "template",
                MetaMessageId = result.MessageId,
                Status = result.Success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow,
                FailedReason = result.Error
            };

            await _repository.AddMessageLogAsync(log);
            return MapLogToDto(log);
        }

        // ===== Private helpers =====

        private async Task<WhatsAppAccount> GetConnectedAccountOrThrow(int businessId)
        {
            var account = await _repository.GetByBusinessIdAsync(businessId);
            if (account == null || account.Status != "Connected")
                throw new InvalidOperationException("WhatsApp is not connected. Please connect via Embedded Signup first.");

            if (string.IsNullOrEmpty(account.PhoneNumberId) || string.IsNullOrEmpty(account.AccessToken))
                throw new InvalidOperationException("WhatsApp account is missing required credentials. Please reconnect.");

            return account;
        }

        private static WhatsAppAccountDto MapToDto(WhatsAppAccount account)
        {
            return new WhatsAppAccountDto
            {
                Id = account.Id,
                DisplayPhoneNumber = account.DisplayPhoneNumber,
                WabaId = account.WabaId,
                PhoneNumberId = account.PhoneNumberId,
                Status = account.Status,
                ConnectedAt = account.ConnectedAt,
                DisconnectedAt = account.DisconnectedAt
            };
        }

        private static MessageLogDto MapLogToDto(MessageLog log)
        {
            return new MessageLogDto
            {
                Id = log.Id,
                BillId = log.BillId,
                RecipientPhone = log.RecipientPhone,
                MessageType = log.MessageType,
                MetaMessageId = log.MetaMessageId,
                Status = log.Status,
                SentAt = log.SentAt,
                DeliveredAt = log.DeliveredAt,
                ReadAt = log.ReadAt,
                FailedReason = log.FailedReason
            };
        }

        private static string FormatWhatsAppPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return string.Empty;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
            {
                return "91" + digits;
            }
            return digits;
        }
    }
}
