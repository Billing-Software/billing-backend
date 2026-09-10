using System;
using System.Linq;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    /// <summary>
    /// Enterprise settings service for managing decoupled, normalized configuration tables:
    /// Payment/Bank, Tax/GST, Invoice Sequence & Rules, Print Layout & Visual Themes, Thermal Printers, and Regional Preferences.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(BillingDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Domain 5: Payment & Bank Details

        public async Task<PaymentSettingsDto> GetPaymentSettingsAsync(int businessId)
        {
            try
            {
                var settings = await _context.BusinessPaymentSettings
                    .FirstOrDefaultAsync(s => s.BusinessId == businessId);

                if (settings != null)
                {
                    return new PaymentSettingsDto
                    {
                        BusinessId = settings.BusinessId,
                        BankName = settings.BankName,
                        AccountHolderName = settings.AccountHolderName,
                        AccountNumber = settings.AccountNumber,
                        IfscCode = settings.IfscCode,
                        BranchName = settings.BranchName,
                        UpiVpa = settings.UpiVpa,
                        ShowUpiQrOnInvoice = settings.ShowUpiQrOnInvoice,
                        ShowBankDetailsOnInvoice = settings.ShowBankDetailsOnInvoice,
                        PaymentGatewayProvider = settings.PaymentGatewayProvider,
                        PaymentGatewayApiKey = settings.PaymentGatewayApiKey,
                        UpdatedAt = settings.UpdatedAt ?? settings.CreatedAt
                    };
                }

                var business = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == businessId);

                return new PaymentSettingsDto
                {
                    BusinessId = businessId,
                    AccountHolderName = business?.LegalName ?? string.Empty,
                    BankName = string.Empty,
                    AccountNumber = string.Empty,
                    IfscCode = string.Empty,
                    BranchName = string.Empty,
                    UpiVpa = string.Empty,
                    ShowUpiQrOnInvoice = true,
                    ShowBankDetailsOnInvoice = false,
                    PaymentGatewayProvider = "None",
                    PaymentGatewayApiKey = string.Empty,
                    UpdatedAt = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve payment settings for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<PaymentSettingsDto> SavePaymentSettingsAsync(int businessId, PaymentSettingsDto dto)
        {
            try
            {
                var settings = await _context.BusinessPaymentSettings
                    .FirstOrDefaultAsync(s => s.BusinessId == businessId);

                if (settings == null)
                {
                    settings = new BusinessPaymentSettings
                    {
                        BusinessId = businessId,
                        BankName = dto.BankName?.Trim(),
                        AccountHolderName = dto.AccountHolderName?.Trim(),
                        AccountNumber = dto.AccountNumber?.Trim(),
                        IfscCode = dto.IfscCode?.Trim().ToUpperInvariant(),
                        BranchName = dto.BranchName?.Trim(),
                        UpiVpa = dto.UpiVpa?.Trim(),
                        ShowUpiQrOnInvoice = dto.ShowUpiQrOnInvoice,
                        ShowBankDetailsOnInvoice = dto.ShowBankDetailsOnInvoice,
                        PaymentGatewayProvider = string.IsNullOrWhiteSpace(dto.PaymentGatewayProvider) ? "None" : dto.PaymentGatewayProvider.Trim(),
                        PaymentGatewayApiKey = dto.PaymentGatewayApiKey?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessPaymentSettings.Add(settings);
                }
                else
                {
                    settings.BankName = dto.BankName?.Trim();
                    settings.AccountHolderName = dto.AccountHolderName?.Trim();
                    settings.AccountNumber = dto.AccountNumber?.Trim();
                    settings.IfscCode = dto.IfscCode?.Trim().ToUpperInvariant();
                    settings.BranchName = dto.BranchName?.Trim();
                    settings.UpiVpa = dto.UpiVpa?.Trim();
                    settings.ShowUpiQrOnInvoice = dto.ShowUpiQrOnInvoice;
                    settings.ShowBankDetailsOnInvoice = dto.ShowBankDetailsOnInvoice;
                    settings.PaymentGatewayProvider = string.IsNullOrWhiteSpace(dto.PaymentGatewayProvider) ? "None" : dto.PaymentGatewayProvider.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.PaymentGatewayApiKey))
                    {
                        settings.PaymentGatewayApiKey = dto.PaymentGatewayApiKey.Trim();
                    }
                    settings.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetPaymentSettingsAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save payment settings for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion

        #region Domain 4: Tax & GST Compliance

        public async Task<TaxSettingsDto> GetTaxSettingsAsync(int businessId)
        {
            try
            {
                var tax = await _context.BusinessTaxSettings
                    .FirstOrDefaultAsync(t => t.BusinessId == businessId);

                if (tax != null)
                {
                    return new TaxSettingsDto
                    {
                        IsGstRegistered = tax.IsGstRegistered,
                        GstIn = tax.GstIn,
                        GstScheme = tax.GstScheme,
                        PanNumber = tax.PanNumber,
                        RegisteredState = tax.RegisteredState,
                        RegisteredStateCode = tax.RegisteredStateCode,
                        DefaultTaxRate = tax.DefaultTaxRate,
                        PricesIncludeTax = tax.PricesIncludeTax,
                        TaxFilingFrequency = tax.TaxFilingFrequency,
                        EnableReverseCharge = tax.EnableReverseCharge,
                        EnableEInvoicing = tax.EnableEInvoicing,
                        EWayBillThreshold = tax.EWayBillThreshold
                    };
                }

                // Fallback to Business record if not yet migrated
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
                var hasGstIn = !string.IsNullOrWhiteSpace(business?.GstIn) && business!.GstIn.Trim().Length >= 15;
                var isGst = hasGstIn && (business?.GstScheme != "None" && business?.GstScheme != "Non-GST");

                return new TaxSettingsDto
                {
                    IsGstRegistered = isGst,
                    GstIn = isGst ? business?.GstIn : null,
                    GstScheme = isGst ? (business?.GstScheme ?? "Regular") : "None",
                    PanNumber = null,
                    RegisteredState = business?.RegisteredState,
                    RegisteredStateCode = null,
                    DefaultTaxRate = isGst ? (business?.DefaultTaxRate ?? 18.00m) : 0.00m,
                    PricesIncludeTax = business?.PricesIncludeTax ?? true,
                    TaxFilingFrequency = "Monthly",
                    EnableReverseCharge = false,
                    EnableEInvoicing = false,
                    EWayBillThreshold = 50000.00m
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve tax settings for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<TaxSettingsDto> SaveTaxSettingsAsync(int businessId, TaxSettingsDto dto)
        {
            try
            {
                var tax = await _context.BusinessTaxSettings
                    .FirstOrDefaultAsync(t => t.BusinessId == businessId);

                var isRegistered = dto.IsGstRegistered && !string.IsNullOrWhiteSpace(dto.GstIn) && dto.GstIn.Trim().Length >= 15;
                var scheme = isRegistered ? (string.IsNullOrWhiteSpace(dto.GstScheme) ? "Regular" : dto.GstScheme.Trim()) : "None";
                var rate = isRegistered ? (dto.DefaultTaxRate > 0 ? dto.DefaultTaxRate : 18.00m) : 0.00m;

                if (tax == null)
                {
                    tax = new BusinessTaxSettings
                    {
                        BusinessId = businessId,
                        IsGstRegistered = isRegistered,
                        GstIn = isRegistered ? dto.GstIn?.Trim().ToUpperInvariant() : null,
                        GstScheme = scheme,
                        PanNumber = dto.PanNumber?.Trim().ToUpperInvariant(),
                        RegisteredState = dto.RegisteredState?.Trim(),
                        RegisteredStateCode = dto.RegisteredStateCode?.Trim(),
                        DefaultTaxRate = rate,
                        PricesIncludeTax = dto.PricesIncludeTax,
                        TaxFilingFrequency = dto.TaxFilingFrequency ?? "Monthly",
                        EnableReverseCharge = dto.EnableReverseCharge,
                        EnableEInvoicing = dto.EnableEInvoicing,
                        EWayBillThreshold = dto.EWayBillThreshold > 0 ? dto.EWayBillThreshold : 50000.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessTaxSettings.Add(tax);
                }
                else
                {
                    tax.IsGstRegistered = isRegistered;
                    tax.GstIn = isRegistered ? dto.GstIn?.Trim().ToUpperInvariant() : null;
                    tax.GstScheme = scheme;
                    tax.PanNumber = dto.PanNumber?.Trim().ToUpperInvariant();
                    tax.RegisteredState = dto.RegisteredState?.Trim();
                    tax.RegisteredStateCode = dto.RegisteredStateCode?.Trim();
                    tax.DefaultTaxRate = rate;
                    tax.PricesIncludeTax = dto.PricesIncludeTax;
                    tax.TaxFilingFrequency = dto.TaxFilingFrequency ?? "Monthly";
                    tax.EnableReverseCharge = dto.EnableReverseCharge;
                    tax.EnableEInvoicing = dto.EnableEInvoicing;
                    tax.EWayBillThreshold = dto.EWayBillThreshold > 0 ? dto.EWayBillThreshold : 50000.00m;
                    tax.UpdatedAt = DateTime.UtcNow;
                }

                // Synchronize backwards-compatible fields on Business
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
                if (business != null)
                {
                    business.GstIn = tax.GstIn;
                    business.GstScheme = tax.GstScheme;
                    business.DefaultTaxRate = tax.DefaultTaxRate;
                    business.PricesIncludeTax = tax.PricesIncludeTax;
                    business.RegisteredState = tax.RegisteredState;
                    business.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetTaxSettingsAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save tax settings for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion

        #region Domain 3: Invoice Numbering & Billing Rules

        public async Task<InvoiceSettingsDto> GetInvoiceSettingsAsync(int businessId)
        {
            try
            {
                var inv = await _context.BusinessInvoiceSettings
                    .FirstOrDefaultAsync(i => i.BusinessId == businessId);

                if (inv != null)
                {
                    return new InvoiceSettingsDto
                    {
                        InvoicePrefix = inv.InvoicePrefix,
                        StartingInvoiceNumber = inv.StartingInvoiceNumber,
                        CurrentSequenceNumber = inv.CurrentSequenceNumber,
                        InvoiceNumberFormat = inv.InvoiceNumberFormat,
                        DefaultCurrency = inv.DefaultCurrency,
                        DefaultPaymentTerms = inv.DefaultPaymentTerms,
                        InvoiceDueDays = inv.InvoiceDueDays,
                        DefaultNotes = inv.DefaultNotes,
                        TermsAndConditions = inv.TermsAndConditions,
                        AutoRoundOff = inv.AutoRoundOff
                    };
                }

                return new InvoiceSettingsDto
                {
                    InvoicePrefix = "INV-",
                    StartingInvoiceNumber = 1001,
                    CurrentSequenceNumber = 1000,
                    InvoiceNumberFormat = "INV-XXXX",
                    DefaultCurrency = "INR (₹)",
                    DefaultPaymentTerms = "Due on Receipt",
                    InvoiceDueDays = 0,
                    DefaultNotes = "Thank you for your business!",
                    TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Warranty claims subject to manufacturer terms.",
                    AutoRoundOff = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve invoice settings for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<InvoiceSettingsDto> SaveInvoiceSettingsAsync(int businessId, InvoiceSettingsDto dto)
        {
            try
            {
                var inv = await _context.BusinessInvoiceSettings
                    .FirstOrDefaultAsync(i => i.BusinessId == businessId);

                if (inv == null)
                {
                    inv = new BusinessInvoiceSettings
                    {
                        BusinessId = businessId,
                        InvoicePrefix = string.IsNullOrWhiteSpace(dto.InvoicePrefix) ? "INV-" : dto.InvoicePrefix.Trim().ToUpperInvariant(),
                        StartingInvoiceNumber = dto.StartingInvoiceNumber > 0 ? dto.StartingInvoiceNumber : 1001,
                        CurrentSequenceNumber = dto.CurrentSequenceNumber >= dto.StartingInvoiceNumber ? dto.CurrentSequenceNumber : dto.StartingInvoiceNumber - 1,
                        InvoiceNumberFormat = string.IsNullOrWhiteSpace(dto.InvoiceNumberFormat) ? "INV-XXXX" : dto.InvoiceNumberFormat.Trim(),
                        DefaultCurrency = string.IsNullOrWhiteSpace(dto.DefaultCurrency) ? "INR (₹)" : dto.DefaultCurrency.Trim(),
                        DefaultPaymentTerms = string.IsNullOrWhiteSpace(dto.DefaultPaymentTerms) ? "Due on Receipt" : dto.DefaultPaymentTerms.Trim(),
                        InvoiceDueDays = dto.InvoiceDueDays,
                        DefaultNotes = dto.DefaultNotes?.Trim(),
                        TermsAndConditions = dto.TermsAndConditions?.Trim(),
                        AutoRoundOff = dto.AutoRoundOff,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessInvoiceSettings.Add(inv);
                }
                else
                {
                    inv.InvoicePrefix = string.IsNullOrWhiteSpace(dto.InvoicePrefix) ? "INV-" : dto.InvoicePrefix.Trim().ToUpperInvariant();
                    inv.StartingInvoiceNumber = dto.StartingInvoiceNumber > 0 ? dto.StartingInvoiceNumber : 1001;
                    inv.InvoiceNumberFormat = string.IsNullOrWhiteSpace(dto.InvoiceNumberFormat) ? "INV-XXXX" : dto.InvoiceNumberFormat.Trim();
                    inv.DefaultCurrency = string.IsNullOrWhiteSpace(dto.DefaultCurrency) ? "INR (₹)" : dto.DefaultCurrency.Trim();
                    inv.DefaultPaymentTerms = string.IsNullOrWhiteSpace(dto.DefaultPaymentTerms) ? "Due on Receipt" : dto.DefaultPaymentTerms.Trim();
                    inv.InvoiceDueDays = dto.InvoiceDueDays;
                    inv.DefaultNotes = dto.DefaultNotes?.Trim();
                    inv.TermsAndConditions = dto.TermsAndConditions?.Trim();
                    inv.AutoRoundOff = dto.AutoRoundOff;
                    inv.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetInvoiceSettingsAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save invoice settings for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion

        #region Domain 6: Invoice Visual Themes & Designer

        public async Task<InvoiceDesignDto> GetInvoiceDesignAsync(int businessId)
        {
            try
            {
                var des = await _context.BusinessInvoiceDesigns
                    .FirstOrDefaultAsync(d => d.BusinessId == businessId);

                if (des != null)
                {
                    return new InvoiceDesignDto
                    {
                        ThemeId = des.ThemeId,
                        PaperSize = des.PaperSize,
                        BrandColorHex = des.BrandColorHex,
                        StoreDisplayName = des.StoreDisplayName,
                        Tagline = des.Tagline,
                        ShowLogo = des.ShowLogo,
                        LogoPosition = des.LogoPosition,
                        ShowGstin = des.ShowGstin,
                        ShowContact = des.ShowContact,
                        ShowSerialNo = des.ShowSerialNo,
                        ShowItemName = des.ShowItemName,
                        ShowHsnSac = des.ShowHsnSac,
                        ShowMrp = des.ShowMrp,
                        ShowDiscount = des.ShowDiscount,
                        ShowTaxRate = des.ShowTaxRate,
                        ShowBatchExpiry = des.ShowBatchExpiry,
                        ShowAmountInWords = des.ShowAmountInWords,
                        ShowPreviousBalance = des.ShowPreviousBalance,
                        ShowTaxBreakdown = des.ShowTaxBreakdown,
                        ShowSignature = des.ShowSignature,
                        SignatoryTitle = des.SignatoryTitle,
                        FooterMessage = des.FooterMessage
                    };
                }

                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);

                return new InvoiceDesignDto
                {
                    ThemeId = "modern",
                    PaperSize = business?.ReceiptTemplateType ?? "Thermal80mm",
                    BrandColorHex = "#006A61",
                    StoreDisplayName = business?.TradingName ?? business?.LegalName,
                    Tagline = business?.ReceiptHeader ?? "Tax Invoice / Bill of Supply",
                    ShowLogo = business?.ShowLogoOnReceipt ?? true,
                    LogoPosition = "left",
                    ShowGstin = true,
                    ShowContact = true,
                    ShowSerialNo = true,
                    ShowItemName = true,
                    ShowHsnSac = true,
                    ShowMrp = true,
                    ShowDiscount = true,
                    ShowTaxRate = true,
                    ShowBatchExpiry = false,
                    ShowAmountInWords = true,
                    ShowPreviousBalance = true,
                    ShowTaxBreakdown = true,
                    ShowSignature = true,
                    SignatoryTitle = "Authorized Signatory",
                    FooterMessage = business?.ReceiptFooter ?? "Thank you for your business! Visit again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve invoice design for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<InvoiceDesignDto> SaveInvoiceDesignAsync(int businessId, InvoiceDesignDto dto)
        {
            try
            {
                var des = await _context.BusinessInvoiceDesigns
                    .FirstOrDefaultAsync(d => d.BusinessId == businessId);

                if (des == null)
                {
                    des = new BusinessInvoiceDesign
                    {
                        BusinessId = businessId,
                        ThemeId = string.IsNullOrWhiteSpace(dto.ThemeId) ? "modern" : dto.ThemeId.Trim(),
                        PaperSize = string.IsNullOrWhiteSpace(dto.PaperSize) ? "Thermal80mm" : dto.PaperSize.Trim(),
                        BrandColorHex = string.IsNullOrWhiteSpace(dto.BrandColorHex) ? "#006A61" : dto.BrandColorHex.Trim(),
                        StoreDisplayName = dto.StoreDisplayName?.Trim(),
                        Tagline = dto.Tagline?.Trim(),
                        ShowLogo = dto.ShowLogo,
                        LogoPosition = string.IsNullOrWhiteSpace(dto.LogoPosition) ? "left" : dto.LogoPosition.Trim(),
                        ShowGstin = dto.ShowGstin,
                        ShowContact = dto.ShowContact,
                        ShowSerialNo = dto.ShowSerialNo,
                        ShowItemName = dto.ShowItemName,
                        ShowHsnSac = dto.ShowHsnSac,
                        ShowMrp = dto.ShowMrp,
                        ShowDiscount = dto.ShowDiscount,
                        ShowTaxRate = dto.ShowTaxRate,
                        ShowBatchExpiry = dto.ShowBatchExpiry,
                        ShowAmountInWords = dto.ShowAmountInWords,
                        ShowPreviousBalance = dto.ShowPreviousBalance,
                        ShowTaxBreakdown = dto.ShowTaxBreakdown,
                        ShowSignature = dto.ShowSignature,
                        SignatoryTitle = string.IsNullOrWhiteSpace(dto.SignatoryTitle) ? "Authorized Signatory" : dto.SignatoryTitle.Trim(),
                        FooterMessage = dto.FooterMessage?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessInvoiceDesigns.Add(des);
                }
                else
                {
                    des.ThemeId = string.IsNullOrWhiteSpace(dto.ThemeId) ? "modern" : dto.ThemeId.Trim();
                    des.PaperSize = string.IsNullOrWhiteSpace(dto.PaperSize) ? "Thermal80mm" : dto.PaperSize.Trim();
                    des.BrandColorHex = string.IsNullOrWhiteSpace(dto.BrandColorHex) ? "#006A61" : dto.BrandColorHex.Trim();
                    des.StoreDisplayName = dto.StoreDisplayName?.Trim();
                    des.Tagline = dto.Tagline?.Trim();
                    des.ShowLogo = dto.ShowLogo;
                    des.LogoPosition = string.IsNullOrWhiteSpace(dto.LogoPosition) ? "left" : dto.LogoPosition.Trim();
                    des.ShowGstin = dto.ShowGstin;
                    des.ShowContact = dto.ShowContact;
                    des.ShowSerialNo = dto.ShowSerialNo;
                    des.ShowItemName = dto.ShowItemName;
                    des.ShowHsnSac = dto.ShowHsnSac;
                    des.ShowMrp = dto.ShowMrp;
                    des.ShowDiscount = dto.ShowDiscount;
                    des.ShowTaxRate = dto.ShowTaxRate;
                    des.ShowBatchExpiry = dto.ShowBatchExpiry;
                    des.ShowAmountInWords = dto.ShowAmountInWords;
                    des.ShowPreviousBalance = dto.ShowPreviousBalance;
                    des.ShowTaxBreakdown = dto.ShowTaxBreakdown;
                    des.ShowSignature = dto.ShowSignature;
                    des.SignatoryTitle = string.IsNullOrWhiteSpace(dto.SignatoryTitle) ? "Authorized Signatory" : dto.SignatoryTitle.Trim();
                    des.FooterMessage = dto.FooterMessage?.Trim();
                    des.UpdatedAt = DateTime.UtcNow;
                }

                // Sync receipt columns on Business
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
                if (business != null)
                {
                    business.ReceiptTemplateType = des.PaperSize;
                    business.ReceiptHeader = des.Tagline;
                    business.ReceiptFooter = des.FooterMessage;
                    business.ShowLogoOnReceipt = des.ShowLogo;
                    business.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetInvoiceDesignAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save invoice design for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion

        #region Hardware: Thermal POS Printer Settings

        public async Task<PrinterSettingsDto> GetPrinterSettingsAsync(int businessId, int? branchId = null)
        {
            try
            {
                var printer = await _context.BusinessPrinterSettings
                    .Where(p => p.BusinessId == businessId && (branchId == null || p.BranchId == branchId))
                    .OrderByDescending(p => p.BranchId == branchId)
                    .FirstOrDefaultAsync();

                if (printer != null)
                {
                    return new PrinterSettingsDto
                    {
                        Id = printer.Id,
                        BranchId = printer.BranchId,
                        PrinterName = printer.PrinterName,
                        PrinterType = printer.PrinterType,
                        MacAddressOrIp = printer.MacAddressOrIp,
                        PaperWidthMm = printer.PaperWidthMm,
                        AutoPrintOnBillComplete = printer.AutoPrintOnBillComplete,
                        NumberOfCopies = printer.NumberOfCopies,
                        FeedLinesAfterPrint = printer.FeedLinesAfterPrint,
                        CutPaperEnabled = printer.CutPaperEnabled,
                        OpenCashDrawerEnabled = printer.OpenCashDrawerEnabled,
                        IsActive = printer.IsActive
                    };
                }

                return new PrinterSettingsDto
                {
                    PrinterName = "Bluetooth Thermal Printer",
                    PrinterType = "BluetoothThermal",
                    PaperWidthMm = 80,
                    AutoPrintOnBillComplete = true,
                    NumberOfCopies = 1,
                    FeedLinesAfterPrint = 2,
                    CutPaperEnabled = true,
                    OpenCashDrawerEnabled = false,
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve printer settings for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<PrinterSettingsDto> SavePrinterSettingsAsync(int businessId, PrinterSettingsDto dto)
        {
            try
            {
                // IDOR guard: BranchId must belong to this business.
                if (dto.BranchId.HasValue)
                {
                    var branchOk = await _context.Branches.AnyAsync(b => b.Id == dto.BranchId.Value && b.BusinessId == businessId);
                    if (!branchOk)
                        throw new InvalidOperationException("Selected branch does not belong to this business.");
                }
                var printer = await _context.BusinessPrinterSettings
                    .FirstOrDefaultAsync(p => p.BusinessId == businessId && (dto.Id != null ? p.Id == dto.Id : p.BranchId == dto.BranchId));

                if (printer == null)
                {
                    printer = new BusinessPrinterSettings
                    {
                        BusinessId = businessId,
                        BranchId = dto.BranchId,
                        PrinterName = string.IsNullOrWhiteSpace(dto.PrinterName) ? "POS Thermal Printer" : dto.PrinterName.Trim(),
                        PrinterType = string.IsNullOrWhiteSpace(dto.PrinterType) ? "BluetoothThermal" : dto.PrinterType.Trim(),
                        MacAddressOrIp = dto.MacAddressOrIp?.Trim(),
                        PaperWidthMm = dto.PaperWidthMm == 58 ? 58 : 80,
                        AutoPrintOnBillComplete = dto.AutoPrintOnBillComplete,
                        NumberOfCopies = dto.NumberOfCopies > 0 ? dto.NumberOfCopies : 1,
                        FeedLinesAfterPrint = dto.FeedLinesAfterPrint >= 0 ? dto.FeedLinesAfterPrint : 2,
                        CutPaperEnabled = dto.CutPaperEnabled,
                        OpenCashDrawerEnabled = dto.OpenCashDrawerEnabled,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessPrinterSettings.Add(printer);
                }
                else
                {
                    printer.PrinterName = string.IsNullOrWhiteSpace(dto.PrinterName) ? "POS Thermal Printer" : dto.PrinterName.Trim();
                    printer.PrinterType = string.IsNullOrWhiteSpace(dto.PrinterType) ? "BluetoothThermal" : dto.PrinterType.Trim();
                    printer.MacAddressOrIp = dto.MacAddressOrIp?.Trim();
                    printer.PaperWidthMm = dto.PaperWidthMm == 58 ? 58 : 80;
                    printer.AutoPrintOnBillComplete = dto.AutoPrintOnBillComplete;
                    printer.NumberOfCopies = dto.NumberOfCopies > 0 ? dto.NumberOfCopies : 1;
                    printer.FeedLinesAfterPrint = dto.FeedLinesAfterPrint >= 0 ? dto.FeedLinesAfterPrint : 2;
                    printer.CutPaperEnabled = dto.CutPaperEnabled;
                    printer.OpenCashDrawerEnabled = dto.OpenCashDrawerEnabled;
                    printer.IsActive = dto.IsActive;
                    printer.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetPrinterSettingsAsync(businessId, dto.BranchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save printer settings for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion

        #region App Preferences & Regional Settings

        public async Task<AppPreferencesDto> GetAppPreferencesAsync(int businessId)
        {
            try
            {
                var pref = await _context.BusinessAppPreferences
                    .FirstOrDefaultAsync(p => p.BusinessId == businessId);

                if (pref != null)
                {
                    return new AppPreferencesDto
                    {
                        DefaultLanguage = pref.DefaultLanguage,
                        DateFormat = pref.DateFormat,
                        TimeFormat = pref.TimeFormat,
                        CurrencySymbol = pref.CurrencySymbol,
                        CurrencyPlacement = pref.CurrencyPlacement,
                        EnableSoundEffects = pref.EnableSoundEffects,
                        EnableHapticFeedback = pref.EnableHapticFeedback,
                        BarcodeScannerMode = pref.BarcodeScannerMode
                    };
                }

                return new AppPreferencesDto
                {
                    DefaultLanguage = "en",
                    DateFormat = "dd/MM/yyyy",
                    TimeFormat = "12h",
                    CurrencySymbol = "₹",
                    CurrencyPlacement = "BeforeAmount",
                    EnableSoundEffects = true,
                    EnableHapticFeedback = true,
                    BarcodeScannerMode = "AutoDetect"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to retrieve app preferences for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<AppPreferencesDto> SaveAppPreferencesAsync(int businessId, AppPreferencesDto dto)
        {
            try
            {
                var pref = await _context.BusinessAppPreferences
                    .FirstOrDefaultAsync(p => p.BusinessId == businessId);

                if (pref == null)
                {
                    pref = new BusinessAppPreferences
                    {
                        BusinessId = businessId,
                        DefaultLanguage = string.IsNullOrWhiteSpace(dto.DefaultLanguage) ? "en" : dto.DefaultLanguage.Trim(),
                        DateFormat = string.IsNullOrWhiteSpace(dto.DateFormat) ? "dd/MM/yyyy" : dto.DateFormat.Trim(),
                        TimeFormat = string.IsNullOrWhiteSpace(dto.TimeFormat) ? "12h" : dto.TimeFormat.Trim(),
                        CurrencySymbol = string.IsNullOrWhiteSpace(dto.CurrencySymbol) ? "₹" : dto.CurrencySymbol.Trim(),
                        CurrencyPlacement = string.IsNullOrWhiteSpace(dto.CurrencyPlacement) ? "BeforeAmount" : dto.CurrencyPlacement.Trim(),
                        EnableSoundEffects = dto.EnableSoundEffects,
                        EnableHapticFeedback = dto.EnableHapticFeedback,
                        BarcodeScannerMode = string.IsNullOrWhiteSpace(dto.BarcodeScannerMode) ? "AutoDetect" : dto.BarcodeScannerMode.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BusinessAppPreferences.Add(pref);
                }
                else
                {
                    pref.DefaultLanguage = string.IsNullOrWhiteSpace(dto.DefaultLanguage) ? "en" : dto.DefaultLanguage.Trim();
                    pref.DateFormat = string.IsNullOrWhiteSpace(dto.DateFormat) ? "dd/MM/yyyy" : dto.DateFormat.Trim();
                    pref.TimeFormat = string.IsNullOrWhiteSpace(dto.TimeFormat) ? "12h" : dto.TimeFormat.Trim();
                    pref.CurrencySymbol = string.IsNullOrWhiteSpace(dto.CurrencySymbol) ? "₹" : dto.CurrencySymbol.Trim();
                    pref.CurrencyPlacement = string.IsNullOrWhiteSpace(dto.CurrencyPlacement) ? "BeforeAmount" : dto.CurrencyPlacement.Trim();
                    pref.EnableSoundEffects = dto.EnableSoundEffects;
                    pref.EnableHapticFeedback = dto.EnableHapticFeedback;
                    pref.BarcodeScannerMode = string.IsNullOrWhiteSpace(dto.BarcodeScannerMode) ? "AutoDetect" : dto.BarcodeScannerMode.Trim();
                    pref.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return await GetAppPreferencesAsync(businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SettingsService] Failed to save app preferences for business {BusinessId}", businessId);
                throw;
            }
        }

        #endregion
    }
}
