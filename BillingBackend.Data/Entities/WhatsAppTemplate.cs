using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class WhatsAppTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WhatsAppSettingsId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        // Navigation
        [ForeignKey(nameof(WhatsAppSettingsId))]
        public WhatsAppSettings WhatsAppSettings { get; set; } = null!;
    }
}
