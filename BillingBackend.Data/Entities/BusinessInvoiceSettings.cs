using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Domain 3: Invoice Prefix, Numbering Sequence, Currency, and Payment Terms.
    /// Replaces mobile SharedPreferences with authoritative database persistence.
    /// </summary>
    public class BusinessInvoiceSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(20)]
        public string InvoicePrefix { get; set; } = "INV-";

        public int StartingInvoiceNumber { get; set; } = 1001;

        public int CurrentSequenceNumber { get; set; } = 1000;

        [Required]
        [MaxLength(50)]
        public string InvoiceNumberFormat { get; set; } = "INV-XXXX"; // INV-XXXX, INV-YYYY-XXXX, PREFIX-XXXX

        [Required]
        [MaxLength(20)]
        public string DefaultCurrency { get; set; } = "INR (₹)";

        [Required]
        [MaxLength(50)]
        public string DefaultPaymentTerms { get; set; } = "Due on Receipt"; // Due on Receipt, Net 7, Net 15, Net 30, Net 60

        public int InvoiceDueDays { get; set; } = 0;

        [MaxLength(500)]
        public string? DefaultNotes { get; set; } = "Thank you for your business!";

        public string? TermsAndConditions { get; set; } = "1. Goods once sold will not be taken back.\n2. Warranty claims subject to manufacturer terms.";

        public bool AutoRoundOff { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
