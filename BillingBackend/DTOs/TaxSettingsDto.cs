using System.ComponentModel.DataAnnotations;
using BillingBackend.Validation;

namespace BillingBackend.DTOs
{
    public class TaxSettingsDto
    {
        public bool IsGstRegistered { get; set; }

        [MaxLength(15)]
        [RegularExpression(ValidationPatterns.Gstin, ErrorMessage = "GstIn must be a valid 15-character GSTIN.")]
        public string? GstIn { get; set; }

        [Required]
        [MaxLength(50)]
        public string GstScheme { get; set; } = "None"; // None, Regular, Composition

        [MaxLength(20)]
        [RegularExpression(ValidationPatterns.Pan, ErrorMessage = "PanNumber must be a valid 10-character PAN (e.g. ABCDE1234F).")]
        public string? PanNumber { get; set; }

        [MaxLength(100)]
        public string? RegisteredState { get; set; }

        [MaxLength(10)]
        [RegularExpression(ValidationPatterns.GstStateCode, ErrorMessage = "RegisteredStateCode must be a valid 2-digit GST state code (01-37).")]
        public string? RegisteredStateCode { get; set; }

        [Range(0, 28, ErrorMessage = "DefaultTaxRate must be between 0 and 28.")]
        public decimal DefaultTaxRate { get; set; } = 0.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [MaxLength(50)]
        public string TaxFilingFrequency { get; set; } = "Monthly";

        public bool EnableReverseCharge { get; set; } = false;

        public bool EnableEInvoicing { get; set; } = false;

        [Range(0, 10000000, ErrorMessage = "EWayBillThreshold must be non-negative.")]
        public decimal EWayBillThreshold { get; set; } = 50000.00m;
    }
}
