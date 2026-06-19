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

        [Required]
        public List<BillItemDto> Items { get; set; } = new List<BillItemDto>();
    }
}
