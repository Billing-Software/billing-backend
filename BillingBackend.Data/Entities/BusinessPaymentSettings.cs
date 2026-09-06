using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Per-tenant payment and banking configuration.
    /// Stores the merchant's Bank Account, IFSC, and standard UPI VPA for dynamic QR generation
    /// and invoice printing, as well as optional payment gateway credentials.
    /// </summary>
    public class BusinessPaymentSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(150)]
        public string? AccountHolderName { get; set; }

        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        [MaxLength(20)]
        public string? IfscCode { get; set; }

        [MaxLength(100)]
        public string? BranchName { get; set; }

        /// <summary>
        /// Merchant UPI ID / VPA (e.g. storename@okaxis, 9876543210@paytm)
        /// Used for generating NPCI standard dynamic UPI QR codes during checkout and on printed receipts.
        /// </summary>
        [MaxLength(100)]
        public string? UpiVpa { get; set; }

        /// <summary>
        /// Whether to print dynamic UPI QR code on generated receipt/thermal invoice.
        /// </summary>
        public bool ShowUpiQrOnInvoice { get; set; } = true;

        /// <summary>
        /// Whether to print bank account and IFSC details on invoices for NEFT/RTGS wire transfers.
        /// </summary>
        public bool ShowBankDetailsOnInvoice { get; set; } = false;

        /// <summary>
        /// Optional online payment gateway provider (None, Razorpay, Cashfree, PhonePe, Paytm).
        /// </summary>
        [MaxLength(50)]
        public string PaymentGatewayProvider { get; set; } = "None";

        /// <summary>
        /// Payment Gateway API Key or Merchant ID.
        /// </summary>
        [MaxLength(200)]
        public string? PaymentGatewayApiKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
