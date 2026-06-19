using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class BusinessDto
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }

        [Required]
        [StringLength(200)]
        public string LegalName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TradingName { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? GstIn { get; set; }

        public decimal DefaultTaxRate { get; set; } = 18.00m;
        public bool PricesIncludeTax { get; set; } = true;
    }
}
