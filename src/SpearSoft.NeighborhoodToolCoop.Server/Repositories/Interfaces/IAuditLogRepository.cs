using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

/// <summary>
/// Not tenant-scoped via RepositoryBase — accepts explicit tenantId so it can
/// be called from Hangfire jobs that run outside a request context.
/// </summary>
public interface IAuditLogRepository
{
    Task                            AppendAsync(AuditEntry entry);
    Task<IEnumerable<AuditEntry>>   GetByEntityAsync(Guid tenantId, string entityType, Guid entityId);
    Task<IEnumerable<AuditEntry>>   GetByTenantAsync(Guid tenantId, int limit = 100);
}
