using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class TaxCategory
    {
        [Key]
        public int Id { get; set; }

        public int? BusinessId { get; set; } // Null for system global default categories

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string TaxType { get; set; } = "Goods"; // "Goods" or "Services"

        [MaxLength(20)]
        public string? HSNCode { get; set; }

        [MaxLength(20)]
        public string? SACCode { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal GSTPercentage { get; set; } = 18.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CGSTPercentage { get; set; } = 9.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal SGSTPercentage { get; set; } = 9.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal IGSTPercentage { get; set; } = 18.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CessPercentage { get; set; } = 0.00m;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business? Business { get; set; }
    }
}
