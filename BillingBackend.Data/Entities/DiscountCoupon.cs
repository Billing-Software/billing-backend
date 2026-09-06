using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Promotional Discount Coupons and Schemes applicable to invoices.
    /// </summary>
    public class DiscountCoupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CouponCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "Percentage"; // Percentage, FixedAmount

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrderAmount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddMonths(3);

        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
