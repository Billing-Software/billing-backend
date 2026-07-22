using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class StaffMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EmpCode { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Contact { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Staff";

        public int TotalBills { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RevenueGenerated { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public int? BranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        public ICollection<Bill> CreatedBills { get; set; } = new List<Bill>();
    }
}
