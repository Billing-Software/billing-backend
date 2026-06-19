using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class StaffDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public int? UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EmpCode { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Contact { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "Staff";

        public int TotalBills { get; set; }
        public decimal RevenueGenerated { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";
    }
}
