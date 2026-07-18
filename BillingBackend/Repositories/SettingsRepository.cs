using BillingBackend.Data;

namespace BillingBackend.Repositories
{
    /// <summary>
    /// General settings repository.
    /// WhatsApp settings have been moved to WhatsAppRepository.
    /// This repository is reserved for future non-WhatsApp settings.
    /// </summary>
    public class SettingsRepository : ISettingsRepository
    {
        private readonly BillingDbContext _context;

        public SettingsRepository(BillingDbContext context)
        {
            _context = context;
        }
    }
}
