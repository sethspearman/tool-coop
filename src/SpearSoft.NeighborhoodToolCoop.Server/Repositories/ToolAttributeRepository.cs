using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class ToolAttributeRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), IToolAttributeRepository
{
    public async Task<IEnumerable<ToolAttribute>> GetByToolAsync(Guid toolId)
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<ToolAttribute>(
            "SELECT * FROM tool_attributes WHERE tenant_id = @TenantId AND tool_id = @ToolId ORDER BY key",
            new { TenantId, ToolId = toolId });
    }

    public async Task UpsertAsync(ToolAttribute attribute)
    {
        attribute.TenantId = TenantId;
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            INSERT INTO tool_attributes (tenant_id, tool_id, key, value)
            VALUES (@TenantId, @ToolId, @Key, @Value)
            ON CONFLICT (tool_id, key) DO UPDATE SET value = EXCLUDED.value
            """, attribute);
    }

    public async Task DeleteAsync(Guid toolId, string key)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "DELETE FROM tool_attributes WHERE tenant_id = @TenantId AND tool_id = @ToolId AND key = @Key",
            new { TenantId, ToolId = toolId, Key = key });
    }

    public async Task DeleteAllForToolAsync(Guid toolId)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "DELETE FROM tool_attributes WHERE tenant_id = @TenantId AND tool_id = @ToolId",
            new { TenantId, ToolId = toolId });
    }
}
