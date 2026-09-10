using System.ComponentModel.DataAnnotations;
using BillingBackend.Validation;

namespace BillingBackend.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        [RegularExpression(ValidationPatterns.Username, ErrorMessage = "Username may contain only letters, digits and underscore.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        // Server-assigned. Any client-supplied value is ignored (Owner for self-service).
        public string Role { get; set; } = "Owner";

        // Business Owner Details (Business Details)
        [Required]
        [StringLength(200)]
        public string LegalName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TradingName { get; set; }

        [StringLength(20)]
        [RegularExpression(ValidationPatterns.PhoneIntl, ErrorMessage = "BusinessPhone must be a valid 8-15 digit number.")]
        public string? BusinessPhone { get; set; }

        [StringLength(500)]
        public string? BusinessAddress { get; set; }

        [StringLength(100)]
        public string? BusinessCity { get; set; }

        [StringLength(100)]
        public string? BusinessState { get; set; }

        [StringLength(20)]
        [RegularExpression(ValidationPatterns.PincodeIn, ErrorMessage = "BusinessPostalCode must be a valid 6-digit Indian PIN code.")]
        public string? BusinessPostalCode { get; set; }

        [StringLength(100)]
        public string? BusinessCountry { get; set; } = "India";

        [StringLength(50)]
        [RegularExpression(ValidationPatterns.Gstin, ErrorMessage = "GstIn must be a valid 15-character GSTIN.")]
        public string? GstIn { get; set; }

        [StringLength(500)]
        [Url(ErrorMessage = "LogoUrl must be a valid URL.")]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        [Url(ErrorMessage = "Website must be a valid URL.")]
        public string? Website { get; set; }

        [StringLength(256)]
        [EmailAddress]
        public string? BusinessEmail { get; set; }

        [Range(0, 28, ErrorMessage = "DefaultTaxRate must be between 0 and 28.")]
        public decimal DefaultTaxRate { get; set; } = 18.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [StringLength(100)]
        public string BusinessType { get; set; } = "General Retail Store";

        [StringLength(50)]
        public string GstScheme { get; set; } = "Regular";

        [StringLength(100)]
        public string? RegisteredState { get; set; }

        public int? PlanId { get; set; }
    }
}
