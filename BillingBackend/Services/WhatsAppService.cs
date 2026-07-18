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

            // Step 1: Exchange authorization code for access token
            var tokenResponse = await _metaApiClient.ExchangeCodeForTokenAsync(dto.Code);
            if (!string.IsNullOrEmpty(tokenResponse.Error) || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException($"Failed to exchange code: {tokenResponse.Error}");
            }

            // Step 2: Get WABA and phone number info
            var bizInfo = await _metaApiClient.GetSharedWabaInfoAsync(tokenResponse.AccessToken);

            // Step 3: Create or update the WhatsApp account
            var account = await _repository.GetByBusinessIdAsync(businessId);
            if (account == null)
            {
                account = new WhatsAppAccount
                {
                    BusinessId = businessId
                };
            }

            account.MetaBusinessId = bizInfo.BusinessId;
            account.WabaId = bizInfo.WabaId;
            account.PhoneNumberId = bizInfo.PhoneNumberId;
            account.DisplayPhoneNumber = bizInfo.DisplayPhoneNumber;
            account.AccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
            account.TokenExpiry = tokenResponse.ExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                : null;
            account.Status = "Connected";
            account.ConnectedAt = DateTime.UtcNow;
            account.DisconnectedAt = null;

            await _repository.CreateOrUpdateAsync(account);

            _logger.LogInformation("WhatsApp connected successfully for BusinessId: {BusinessId}, WABA: {WabaId}",
                businessId, account.WabaId);

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

            var result = await _metaApiClient.SendTextMessageAsync(
                account.PhoneNumberId!, decryptedToken, dto.Phone, dto.Message);

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
                throw new InvalidOperationException($"Bill {dto.BillId} does not have a PDF generated.");

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
    }
}
