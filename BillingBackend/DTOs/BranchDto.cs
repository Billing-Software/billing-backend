using System.ComponentModel.DataAnnotations;
using BillingBackend.Validation;

namespace BillingBackend.DTOs
{
    public class BranchDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        [RegularExpression(ValidationPatterns.PincodeIn, ErrorMessage = "PostalCode must be a valid 6-digit Indian PIN code.")]
        public string? PostalCode { get; set; }

        [StringLength(20)]
        [RegularExpression(ValidationPatterns.PhoneIntl, ErrorMessage = "Phone must be a valid phone number.")]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
