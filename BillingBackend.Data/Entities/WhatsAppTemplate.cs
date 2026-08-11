using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class WhatsAppTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WhatsAppAccountId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Language { get; set; } = "en";

        [Required]
        [MaxLength(30)]
        public string Category { get; set; } = "UTILITY";

        [MaxLength(2000)]
        public string? BodyText { get; set; }

        /// <summary>
        /// Template status from Meta: APPROVED, PENDING, REJECTED, PAUSED.
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(WhatsAppAccountId))]
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
    }
}
