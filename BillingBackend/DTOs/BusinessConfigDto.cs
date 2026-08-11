using System.Collections.Generic;

namespace BillingBackend.DTOs
{
    public class TerminologyPairDto
    {
        public string Singular { get; set; } = string.Empty;
        public string Plural { get; set; } = string.Empty;
    }

    public class BusinessTypePresetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string IconName { get; set; } = "Store";
        public string SellingModel { get; set; } = "GOODS_AND_SERVICES";
        public List<string> Aliases { get; set; } = new();
        public Dictionary<string, bool> DefaultFeatures { get; set; } = new();
        public Dictionary<string, TerminologyPairDto> DefaultTerminology { get; set; } = new();
    }

    public class BusinessConfigDto
    {
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = "General Retail Store";
        public string Category { get; set; } = "Retail";
        public string SellingModel { get; set; } = "GOODS_AND_SERVICES";
        public string GstScheme { get; set; } = "Regular";
        public string? GstIn { get; set; }
        public string? RegisteredState { get; set; }
        public bool IsGstEnabled => !string.Equals(GstScheme, "None", System.StringComparison.OrdinalIgnoreCase);

        public Dictionary<string, bool> Features { get; set; } = new();
        public Dictionary<string, TerminologyPairDto> Terminology { get; set; } = new();

        public int OnboardingProgressPercentage { get; set; } = 100;
        public List<string> CompletedSetupSteps { get; set; } = new();
        public List<string> PendingSetupSteps { get; set; } = new();
    }

    public class UpdateBusinessConfigDto
    {
        public string? BusinessType { get; set; }
        public string? SellingModel { get; set; }
        public string? GstScheme { get; set; }
        public string? GstIn { get; set; }
        public string? RegisteredState { get; set; }
        public Dictionary<string, bool>? Features { get; set; }
        public Dictionary<string, TerminologyPairDto>? Terminology { get; set; }
    }

    public class TaxCategoryDto
    {
        public int Id { get; set; }
        public int? BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxType { get; set; } = "Goods";
        public string? HSNCode { get; set; }
        public string? SACCode { get; set; }
        public decimal GSTPercentage { get; set; }
        public decimal CGSTPercentage { get; set; }
        public decimal SGSTPercentage { get; set; }
        public decimal IGSTPercentage { get; set; }
        public decimal CessPercentage { get; set; }
        public bool IsActive { get; set; }
    }

    public class TaxCalculationRequestDto
    {
        public string SupplierState { get; set; } = "Andhra Pradesh";
        public string CustomerState { get; set; } = "Andhra Pradesh";
        public List<TaxLineItemRequestDto> Items { get; set; } = new();
    }

    public class TaxLineItemRequestDto
    {
        public int ServiceOrInventoryId { get; set; }
        public string ItemType { get; set; } = "Inventory"; // Inventory or Service
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
        public int? TaxCategoryId { get; set; }
        public string? CustomHSNSAC { get; set; }
    }

    public class TaxCalculationResultDto
    {
        public decimal TaxableAmount { get; set; }
        public decimal TotalCGST { get; set; }
        public decimal TotalSGST { get; set; }
        public decimal TotalIGST { get; set; }
        public decimal TotalCess { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public bool IsInterState { get; set; }
        public List<TaxLineItemResultDto> LineItems { get; set; } = new();
    }

    public class TaxLineItemResultDto
    {
        public int ServiceOrInventoryId { get; set; }
        public string ItemType { get; set; } = "Inventory";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TaxableValue { get; set; }
        public string? HSNCode { get; set; }
        public string? SACCode { get; set; }
        public decimal TaxRate { get; set; }
        public decimal CGSTRate { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTRate { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTRate { get; set; }
        public decimal IGSTAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
