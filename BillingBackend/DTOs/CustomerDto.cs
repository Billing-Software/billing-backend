using System.ComponentModel.DataAnnotations;
using BillingBackend.Validation;

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
        [RegularExpression(ValidationPatterns.PhoneIntl, ErrorMessage = "Phone must be a valid 8-15 digit number, optionally starting with +.")]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        public bool IsWalkIn { get; set; }
    }
}
