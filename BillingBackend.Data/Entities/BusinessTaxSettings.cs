using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Domain 4: Statutory Tax & GST compliance configuration for a business tenant.
    /// Fully decoupled from generic business profile into a dedicated normalized table.
    /// </summary>
    public class BusinessTaxSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        public bool IsGstRegistered { get; set; } = false;

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
        public string? RegisteredStateCode { get; set; } // e.g. 36 for Telangana, 27 for Maharashtra

        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultTaxRate { get; set; } = 0.00m;

        public bool PricesIncludeTax { get; set; } = true;

        [MaxLength(50)]
        public string TaxFilingFrequency { get; set; } = "Monthly"; // Monthly, Quarterly

        public bool EnableReverseCharge { get; set; } = false;

        public bool EnableEInvoicing { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal EWayBillThreshold { get; set; } = 50000.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
