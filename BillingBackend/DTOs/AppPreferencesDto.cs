using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class AppPreferencesDto
    {
        [Required]
        [MaxLength(10)]
        public string DefaultLanguage { get; set; } = "en";

        [Required]
        [MaxLength(50)]
        public string DateFormat { get; set; } = "dd/MM/yyyy";

        [Required]
        [MaxLength(20)]
        public string TimeFormat { get; set; } = "12h";

        [Required]
        [MaxLength(10)]
        public string CurrencySymbol { get; set; } = "₹";

        [Required]
        [MaxLength(20)]
        public string CurrencyPlacement { get; set; } = "BeforeAmount";

        public bool EnableSoundEffects { get; set; } = true;

        public bool EnableHapticFeedback { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string BarcodeScannerMode { get; set; } = "AutoDetect";
    }
}
