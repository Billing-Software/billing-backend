using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IAuditService
    {
        /// <summary>
        /// Log an audit event for any business entity change.
        /// </summary>
        Task LogAsync(
            int? businessId,
            string entityType,
            int? entityId,
            string action,
            object? oldValues = null,
            object? newValues = null,
            string? performedBy = null,
            string? description = null);
    }
}
