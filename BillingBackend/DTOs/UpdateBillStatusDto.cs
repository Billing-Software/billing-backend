using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class UpdateBillStatusDto
    {
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Paid";

        [StringLength(20)]
        public string? PaymentMethod { get; set; }

        [StringLength(200)]
        public string? PaymentReference { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
