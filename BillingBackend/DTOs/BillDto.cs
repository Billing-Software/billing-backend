using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class BillItemDto
    {
        public int Id { get; set; }
        public int BillId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal LineTotal { get; set; }
    }

    public class BillDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? CreatedByStaffId { get; set; }

        [Required]
        [StringLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        [StringLength(50)]
        public string? DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }

        // Extra details for UI
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? StaffName { get; set; }
        public string? BranchName { get; set; }

        // Payment tracking fields
        public string? IdempotencyKey { get; set; }
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }

        // Line Items
        public List<BillItemDto> Items { get; set; } = new List<BillItemDto>();
    }

    public class CreateBillDto
    {
        [Required]
        public int BranchId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? CreatedByStaffId { get; set; }

        [Required]
        [StringLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Client-generated UUID to prevent duplicate bill creation.
        /// </summary>
        [StringLength(100)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// External payment reference (UPI transaction ID, card auth code, etc.)
        /// </summary>
        [StringLength(200)]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Staff notes about the transaction.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public List<BillItemDto> Items { get; set; } = new List<BillItemDto>();
    }
}
