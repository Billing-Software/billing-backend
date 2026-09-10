using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Range(0, 100000000, ErrorMessage = "BasePrice must be non-negative.")]
        public decimal BasePrice { get; set; }
        [Range(0, 28, ErrorMessage = "TaxRate must be between 0 and 28.")]
        public decimal TaxRate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
