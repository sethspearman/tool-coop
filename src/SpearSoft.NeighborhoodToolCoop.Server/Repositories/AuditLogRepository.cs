using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

/// <summary>
/// Not tenant-scoped via RepositoryBase. Accepts explicit tenantId so it can be
/// called from Hangfire background jobs that have no request TenantContext.
/// </summary>
public class AuditLogRepository(DbConnectionFactory dbFactory) : IAuditLogRepository
{
    public async Task AppendAsync(AuditEntry entry)
    {
        using var conn = dbFactory.Create();
        await conn.ExecuteAsync("""
            INSERT INTO audit_log
                (tenant_id, actor_id, action, entity_type, entity_id, old_values, new_values, ip_address)
            VALUES
                (@TenantId, @ActorId, @Action, @EntityType, @EntityId,
                 @OldValues::jsonb, @NewValues::jsonb, @IpAddress::inet)
            """, entry);
    }

    public async Task<IEnumerable<AuditEntry>> GetByEntityAsync(Guid tenantId, string entityType, Guid entityId)
    {
        using var conn = dbFactory.Create();
        return await conn.QueryAsync<AuditEntry>(
            """
            SELECT * FROM audit_log
             WHERE tenant_id   = @TenantId
               AND entity_type = @EntityType
               AND entity_id   = @EntityId
             ORDER BY created_utc DESC
            """,
            new { TenantId = tenantId, EntityType = entityType, EntityId = entityId });
    }

    public async Task<IEnumerable<AuditEntry>> GetByTenantAsync(Guid tenantId, int limit = 100)
    {
        using var conn = dbFactory.Create();
        return await conn.QueryAsync<AuditEntry>(
            "SELECT * FROM audit_log WHERE tenant_id = @TenantId ORDER BY created_utc DESC LIMIT @Limit",
            new { TenantId = tenantId, Limit = limit });
    }
}
