using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class WhatsAppSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [MaxLength(256)]
        public string? ApiKey { get; set; }

        public bool IsConnected { get; set; } = false;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        public ICollection<WhatsAppTemplate> Templates { get; set; } = new List<WhatsAppTemplate>();
    }
}
