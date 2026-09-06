using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services
{
    /// <summary>
    /// Enterprise business settings service interface.
    /// Manages decoupled relational settings: Tax, Invoicing, Print Design, Payment/Bank, Thermal Hardware, and Regional Preferences.
    /// </summary>
    public interface ISettingsService
    {
        // Domain 5: Payment & Banking
        Task<PaymentSettingsDto> GetPaymentSettingsAsync(int businessId);
        Task<PaymentSettingsDto> SavePaymentSettingsAsync(int businessId, PaymentSettingsDto dto);

        // Domain 4: Tax & Statutory Compliance
        Task<TaxSettingsDto> GetTaxSettingsAsync(int businessId);
        Task<TaxSettingsDto> SaveTaxSettingsAsync(int businessId, TaxSettingsDto dto);

        // Domain 3: Invoice Numbering & Billing Rules
        Task<InvoiceSettingsDto> GetInvoiceSettingsAsync(int businessId);
        Task<InvoiceSettingsDto> SaveInvoiceSettingsAsync(int businessId, InvoiceSettingsDto dto);

        // Domain 6: Invoice Visual Themes & Layout
        Task<InvoiceDesignDto> GetInvoiceDesignAsync(int businessId);
        Task<InvoiceDesignDto> SaveInvoiceDesignAsync(int businessId, InvoiceDesignDto dto);

        // Hardware: Thermal POS Printer Settings
        Task<PrinterSettingsDto> GetPrinterSettingsAsync(int businessId, int? branchId = null);
        Task<PrinterSettingsDto> SavePrinterSettingsAsync(int businessId, PrinterSettingsDto dto);

        // App Preferences & Regional Formatting
        Task<AppPreferencesDto> GetAppPreferencesAsync(int businessId);
        Task<AppPreferencesDto> SaveAppPreferencesAsync(int businessId, AppPreferencesDto dto);
    }
}
