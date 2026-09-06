using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class DiscountCouponDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string CouponCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "Percentage"; // Percentage, FixedAmount

        [Range(0.01, 100000)]
        public decimal DiscountValue { get; set; }

        public decimal MinimumOrderAmount { get; set; } = 0.00m;

        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidUntil { get; set; }

        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
