using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Line item record for a stock transfer voucher.
    /// </summary>
    public class StockTransferItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockTransferId { get; set; }

        [Required]
        public int InventoryItemId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "PCS";

        [MaxLength(50)]
        public string? BatchNumber { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StockTransferId))]
        public StockTransfer StockTransfer { get; set; } = null!;

        [ForeignKey(nameof(InventoryItemId))]
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}
