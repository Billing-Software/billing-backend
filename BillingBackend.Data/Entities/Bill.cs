using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? CreatedByStaffId { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [MaxLength(50)]
        public string? DiscountCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? InvoicePdfUrl { get; set; }

        /// <summary>
        /// Client-generated UUID to prevent duplicate bill creation on retries/double-clicks.
        /// </summary>
        [MaxLength(100)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// External payment reference: UPI transaction ID, card auth code, etc.
        /// </summary>
        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Staff notes or context about the bill (e.g., "Customer requested discount for loyalty").
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch Branch { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(CreatedByStaffId))]
        public StaffMember? CreatedByStaff { get; set; }

        public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    }
}
