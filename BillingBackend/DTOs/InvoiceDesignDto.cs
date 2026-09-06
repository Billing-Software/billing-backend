using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class InvoiceDesignDto
    {
        [Required]
        [MaxLength(50)]
        public string ThemeId { get; set; } = "modern";

        [Required]
        [MaxLength(50)]
        public string PaperSize { get; set; } = "Thermal80mm";

        [Required]
        [MaxLength(20)]
        public string BrandColorHex { get; set; } = "#006A61";

        [MaxLength(200)]
        public string? StoreDisplayName { get; set; }

        [MaxLength(200)]
        public string? Tagline { get; set; }

        public bool ShowLogo { get; set; } = true;

        [Required]
        [MaxLength(20)]
        public string LogoPosition { get; set; } = "left";

        public bool ShowGstin { get; set; } = true;

        public bool ShowContact { get; set; } = true;

        // Columns
        public bool ShowSerialNo { get; set; } = true;
        public bool ShowItemName { get; set; } = true;
        public bool ShowHsnSac { get; set; } = true;
        public bool ShowMrp { get; set; } = true;
        public bool ShowDiscount { get; set; } = true;
        public bool ShowTaxRate { get; set; } = true;
        public bool ShowBatchExpiry { get; set; } = false;

        // Totals
        public bool ShowAmountInWords { get; set; } = true;
        public bool ShowPreviousBalance { get; set; } = true;
        public bool ShowTaxBreakdown { get; set; } = true;

        // Signatory & Footer
        public bool ShowSignature { get; set; } = true;

        [Required]
        [MaxLength(100)]
        public string SignatoryTitle { get; set; } = "Authorized Signatory";

        [MaxLength(500)]
        public string? FooterMessage { get; set; }
    }
}
