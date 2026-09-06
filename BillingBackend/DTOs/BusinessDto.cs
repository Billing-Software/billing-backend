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

        [StringLength(50)]
        public string GstScheme { get; set; } = "Regular";

        public decimal DefaultTaxRate { get; set; } = 18.00m;
        public bool PricesIncludeTax { get; set; } = true;
        [StringLength(500)]
        public string? ReceiptHeader { get; set; }

        [StringLength(500)]
        public string? ReceiptFooter { get; set; }

        public bool ShowLogoOnReceipt { get; set; } = true;

        [StringLength(50)]
        public string ReceiptTemplateType { get; set; } = "Thermal80mm";

        public int ActivePlanId { get; set; } = 1;
        public int AllowedBranches { get; set; } = 1;
        public int AllowedStaff { get; set; } = 2;
        public string SubscriptionStatus { get; set; } = "Inactive";
        public System.DateTime? SubscriptionExpiresAt { get; set; }
        public bool IsTrial { get; set; } = true;
        public System.DateTime? TrialStartsAt { get; set; }
        public System.DateTime? TrialEndsAt { get; set; }
    }
}
