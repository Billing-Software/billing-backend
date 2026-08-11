using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class SACMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? SearchTerms { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultGSTPercentage { get; set; } = 18.00m;

        public bool IsActive { get; set; } = true;
    }
}
