using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Domain 6: Visual Layout, Brand Color, Template, and Print Visibility Flags.
    /// Synchronizes invoice designer across all web, tablet, and mobile POS terminals.
    /// </summary>
    public class BusinessInvoiceDesign
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ThemeId { get; set; } = "modern"; // modern, classic, minimalist, thermal80, thermal58

        [Required]
        [MaxLength(50)]
        public string PaperSize { get; set; } = "Thermal80mm"; // Thermal80mm, Thermal58mm, StandardA4

        [Required]
        [MaxLength(20)]
        public string BrandColorHex { get; set; } = "#006A61";

        [MaxLength(200)]
        public string? StoreDisplayName { get; set; }

        [MaxLength(200)]
        public string? Tagline { get; set; } = "Tax Invoice / Bill of Supply";

        public bool ShowLogo { get; set; } = true;

        [Required]
        [MaxLength(20)]
        public string LogoPosition { get; set; } = "left"; // left, center, right

        public bool ShowGstin { get; set; } = true;

        public bool ShowContact { get; set; } = true;

        // Column toggles
        public bool ShowSerialNo { get; set; } = true;
        public bool ShowItemName { get; set; } = true;
        public bool ShowHsnSac { get; set; } = true;
        public bool ShowMrp { get; set; } = true;
        public bool ShowDiscount { get; set; } = true;
        public bool ShowTaxRate { get; set; } = true;
        public bool ShowBatchExpiry { get; set; } = false;

        // Totals & statutory toggles
        public bool ShowAmountInWords { get; set; } = true;
        public bool ShowPreviousBalance { get; set; } = true;
        public bool ShowTaxBreakdown { get; set; } = true;

        // Signature & footer
        public bool ShowSignature { get; set; } = true;

        [Required]
        [MaxLength(100)]
        public string SignatoryTitle { get; set; } = "Authorized Signatory";

        [MaxLength(500)]
        public string? FooterMessage { get; set; } = "Thank you for your business! Visit again.";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
