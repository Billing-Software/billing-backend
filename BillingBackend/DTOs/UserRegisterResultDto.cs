namespace BillingBackend.DTOs
{
    public class UserRegisterResultDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public int DefaultBranchId { get; set; }
    }
}
