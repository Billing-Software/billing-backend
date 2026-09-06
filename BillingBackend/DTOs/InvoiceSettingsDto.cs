using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class InvoiceSettingsDto
    {
        [Required]
        [MaxLength(20)]
        public string InvoicePrefix { get; set; } = "INV-";

        public int StartingInvoiceNumber { get; set; } = 1001;

        public int CurrentSequenceNumber { get; set; } = 1000;

        [Required]
        [MaxLength(50)]
        public string InvoiceNumberFormat { get; set; } = "INV-XXXX";

        [Required]
        [MaxLength(20)]
        public string DefaultCurrency { get; set; } = "INR (₹)";

        [Required]
        [MaxLength(50)]
        public string DefaultPaymentTerms { get; set; } = "Due on Receipt";

        public int InvoiceDueDays { get; set; } = 0;

        [MaxLength(500)]
        public string? DefaultNotes { get; set; }

        public string? TermsAndConditions { get; set; }

        public bool AutoRoundOff { get; set; } = true;
    }
}
