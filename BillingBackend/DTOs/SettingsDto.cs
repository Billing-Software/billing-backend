using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class WhatsAppTemplateDto
    {
        public int Id { get; set; }
        public int WhatsAppSettingsId { get; set; }

        [Required]
        public string TemplateName { get; set; } = string.Empty;
    }

    public class WhatsAppSettingsDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string? ApiKey { get; set; }
        public bool IsConnected { get; set; }
        public List<WhatsAppTemplateDto> Templates { get; set; } = new List<WhatsAppTemplateDto>();
    }

    public class UpdateWhatsAppSettingsDto
    {
        public string? ApiKey { get; set; }
        public bool IsConnected { get; set; }
    }

    public class AddWhatsAppTemplateDto
    {
        [Required]
        public string TemplateName { get; set; } = string.Empty;
    }
}
