using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Tenant regional formatting, localization language, and UI interaction preferences.
    /// </summary>
    public class BusinessAppPreferences
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(10)]
        public string DefaultLanguage { get; set; } = "en"; // en, hi, te, ta, kn, mr, gu

        [Required]
        [MaxLength(50)]
        public string DateFormat { get; set; } = "dd/MM/yyyy";

        [Required]
        [MaxLength(20)]
        public string TimeFormat { get; set; } = "12h"; // 12h, 24h

        [Required]
        [MaxLength(10)]
        public string CurrencySymbol { get; set; } = "₹";

        [Required]
        [MaxLength(20)]
        public string CurrencyPlacement { get; set; } = "BeforeAmount"; // BeforeAmount, AfterAmount

        public bool EnableSoundEffects { get; set; } = true;

        public bool EnableHapticFeedback { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string BarcodeScannerMode { get; set; } = "AutoDetect"; // Camera, HardwareLaser, AutoDetect

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
