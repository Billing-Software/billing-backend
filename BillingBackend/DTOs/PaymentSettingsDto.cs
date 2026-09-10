using System;
using System.ComponentModel.DataAnnotations;
using BillingBackend.Validation;

namespace BillingBackend.DTOs
{
    public class PaymentSettingsDto
    {
        public int BusinessId { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(150)]
        public string? AccountHolderName { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[0-9]{9,18}$", ErrorMessage = "AccountNumber must be 9-18 digits.")]
        public string? AccountNumber { get; set; }

        [StringLength(20)]
        [RegularExpression(ValidationPatterns.Ifsc, ErrorMessage = "IfscCode must be valid (e.g. HDFC0001234).")]
        public string? IfscCode { get; set; }

        [StringLength(100)]
        public string? BranchName { get; set; }

        /// <summary>
        /// Merchant UPI ID / VPA for dynamic QR code generation
        /// </summary>
        [StringLength(100)]
        [RegularExpression(ValidationPatterns.UpiVpa, ErrorMessage = "UpiVpa must be a valid VPA (e.g. shop@upi).")]
        public string? UpiVpa { get; set; }

        public bool ShowUpiQrOnInvoice { get; set; } = true;

        public bool ShowBankDetailsOnInvoice { get; set; } = false;

        [StringLength(50)]
        public string PaymentGatewayProvider { get; set; } = "None";

        [StringLength(200)]
        public string? PaymentGatewayApiKey { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
