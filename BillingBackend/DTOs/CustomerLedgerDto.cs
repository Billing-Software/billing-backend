using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class CustomerLedgerDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? BillId { get; set; }
        public string? BillNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = "Debit"; // Debit, Credit, Adjustment

        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMode { get; set; } = "Cash";

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? RecordedByStaffId { get; set; }
        public string? RecordedByStaffName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLedgerEntryDto
    {
        [Required]
        public int CustomerId { get; set; }

        public int? BillId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = "Credit"; // Payment received

        [Range(0.01, 10000000)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMode { get; set; } = "Cash";

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
