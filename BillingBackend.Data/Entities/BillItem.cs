using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class BillItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BillId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        [Required]
        [MaxLength(50)]
        public string ItemType { get; set; } = "Service";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        // Tax & Historical Invoice Snapshots
        [MaxLength(20)]
        public string? HSNCode { get; set; }

        [MaxLength(20)]
        public string? SACCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableValue { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 18.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CGSTRate { get; set; } = 9.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal SGSTRate { get; set; } = 9.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal IGSTRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CessRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CessAmount { get; set; } = 0;

        // Navigation
        [ForeignKey(nameof(BillId))]
        public Bill Bill { get; set; } = null!;

        [ForeignKey(nameof(ServiceId))]
        public Service Service { get; set; } = null!;
    }
}
