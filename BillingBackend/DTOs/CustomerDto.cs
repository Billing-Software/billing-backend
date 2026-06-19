using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        public bool IsWalkIn { get; set; }
    }
}
