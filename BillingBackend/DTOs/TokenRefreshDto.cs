using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class TokenRefreshDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
