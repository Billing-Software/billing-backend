using BillingBackend.Data.Entities;

namespace BillingBackend.Services
{
    public interface ITokenService
    {
        string CreateToken(User user, int businessId);
    }
}
