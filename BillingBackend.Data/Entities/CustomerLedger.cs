using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Financial Ledger / Khata entry tracking credit sales, dues, and payments for customers.
    /// </summary>
    public class CustomerLedger
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? BillId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = "Debit"; // Debit (Sale/Due), Credit (Payment received), Adjustment

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RunningBalance { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Cheque, BankTransfer

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? RecordedByStaffId { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(BillId))]
        public Bill? Bill { get; set; }

        [ForeignKey(nameof(RecordedByStaffId))]
        public StaffMember? RecordedByStaff { get; set; }
    }
}
