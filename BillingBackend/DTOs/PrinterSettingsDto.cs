using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class PrinterSettingsDto
    {
        public int? Id { get; set; }

        public int? BranchId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PrinterName { get; set; } = "POS Thermal Printer";

        [Required]
        [MaxLength(50)]
        public string PrinterType { get; set; } = "BluetoothThermal";

        [MaxLength(100)]
        public string? MacAddressOrIp { get; set; }

        public int PaperWidthMm { get; set; } = 80;

        public bool AutoPrintOnBillComplete { get; set; } = true;

        public int NumberOfCopies { get; set; } = 1;

        public int FeedLinesAfterPrint { get; set; } = 2;

        public bool CutPaperEnabled { get; set; } = true;

        public bool OpenCashDrawerEnabled { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
