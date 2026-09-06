using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class RecordBillPaymentDto
    {
        /// <summary>
        /// Payment method used (UPI, Cash, Card, NetBanking, Cheque, Credit)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = "UPI";

        /// <summary>
        /// External payment reference, e.g. UPI Transaction ID / UTR, Card Auth Code, Cheque No.
        /// </summary>
        [StringLength(200)]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// New status for the bill after payment (defaults to "Paid")
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Paid";

        /// <summary>
        /// Optional cashier notes or payment remark
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
