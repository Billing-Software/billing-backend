using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class WarehouseDto
    {
        public int Id { get; set; }

        public int? BranchId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
