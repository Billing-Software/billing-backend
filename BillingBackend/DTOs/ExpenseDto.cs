using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        public DateTime ExpenseDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
