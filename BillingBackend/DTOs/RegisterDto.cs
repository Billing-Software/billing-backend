using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Owner";

        // Business Owner Details (Business Details)
        [Required]
        [StringLength(200)]
        public string LegalName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TradingName { get; set; }

        [StringLength(20)]
        public string? BusinessPhone { get; set; }

        [StringLength(500)]
        public string? BusinessAddress { get; set; }

        [StringLength(100)]
        public string? BusinessCity { get; set; }

        [StringLength(100)]
        public string? BusinessState { get; set; }

        [StringLength(20)]
        public string? BusinessPostalCode { get; set; }

        [StringLength(100)]
        public string? BusinessCountry { get; set; } = "India";

        [StringLength(50)]
        public string? GstIn { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        public string? Website { get; set; }

        [StringLength(256)]
        [EmailAddress]
        public string? BusinessEmail { get; set; }

        public decimal DefaultTaxRate { get; set; } = 18.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [StringLength(100)]
        public string BusinessType { get; set; } = "General Retail Store";

        [StringLength(50)]
        public string GstScheme { get; set; } = "Regular";

        [StringLength(100)]
        public string? RegisteredState { get; set; }
    }
}
