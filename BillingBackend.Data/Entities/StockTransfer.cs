using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Stock transfer voucher moving inventory items between two warehouses/godowns.
    /// </summary>
    public class StockTransfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransferNumber { get; set; } = string.Empty;

        [Required]
        public int SourceWarehouseId { get; set; }

        [Required]
        public int DestinationWarehouseId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // Draft, InTransit, Completed, Cancelled

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public int? DispatchedByStaffId { get; set; }

        public int? ReceivedByStaffId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(SourceWarehouseId))]
        public Warehouse SourceWarehouse { get; set; } = null!;

        [ForeignKey(nameof(DestinationWarehouseId))]
        public Warehouse DestinationWarehouse { get; set; } = null!;

        [ForeignKey(nameof(DispatchedByStaffId))]
        public StaffMember? DispatchedByStaff { get; set; }

        [ForeignKey(nameof(ReceivedByStaffId))]
        public StaffMember? ReceivedByStaff { get; set; }

        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }
}
