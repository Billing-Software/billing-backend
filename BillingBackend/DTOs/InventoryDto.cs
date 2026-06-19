using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class InventoryDto
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

        public int CurrentStock { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "units";

        public int ReorderLevel { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
