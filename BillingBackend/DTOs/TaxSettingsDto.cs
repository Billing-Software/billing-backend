using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class TaxSettingsDto
    {
        public bool IsGstRegistered { get; set; }

        [MaxLength(15)]
        public string? GstIn { get; set; }

        [Required]
        [MaxLength(50)]
        public string GstScheme { get; set; } = "None"; // None, Regular, Composition

        [MaxLength(20)]
        public string? PanNumber { get; set; }

        [MaxLength(100)]
        public string? RegisteredState { get; set; }

        [MaxLength(10)]
        public string? RegisteredStateCode { get; set; }

        public decimal DefaultTaxRate { get; set; } = 0.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [MaxLength(50)]
        public string TaxFilingFrequency { get; set; } = "Monthly";

        public bool EnableReverseCharge { get; set; } = false;

        public bool EnableEInvoicing { get; set; } = false;

        public decimal EWayBillThreshold { get; set; } = 50000.00m;
    }
}
