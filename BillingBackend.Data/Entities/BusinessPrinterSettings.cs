using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Hardware Thermal Printer configuration per business or branch.
    /// Manages Bluetooth, Network IP, and USB thermal receipt printers.
    /// </summary>
    public class BusinessPrinterSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        public int? BranchId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PrinterName { get; set; } = "POS Thermal Printer";

        [Required]
        [MaxLength(50)]
        public string PrinterType { get; set; } = "BluetoothThermal"; // BluetoothThermal, NetworkThermal, UsbThermal

        [MaxLength(100)]
        public string? MacAddressOrIp { get; set; }

        public int PaperWidthMm { get; set; } = 80; // 58, 80

        public bool AutoPrintOnBillComplete { get; set; } = true;

        public int NumberOfCopies { get; set; } = 1;

        public int FeedLinesAfterPrint { get; set; } = 2;

        public bool CutPaperEnabled { get; set; } = true;

        public bool OpenCashDrawerEnabled { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}
