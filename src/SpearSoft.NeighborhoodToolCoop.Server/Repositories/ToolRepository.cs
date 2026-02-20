using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Text;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class ToolRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), IToolRepository
{
    public async Task<IEnumerable<Tool>> ListAsync(ToolFilter filter)
    {
        var sql = new StringBuilder("""
            SELECT t.id, t.tenant_id, t.name, t.description, t.category,
                   t.owner_type, t.owner_user_id, t.condition, t.location_id,
                   t.qr_code, t.image_url, t.value_estimate, t.deposit_required,
                   t.is_active, t.created_utc, t.updated_utc,
                   l.id, l.tenant_id, l.name, l.code, l.path::text AS path,
                   l.parent_id, l.notes, l.created_utc, l.updated_utc
              FROM tools t
              LEFT JOIN locations l ON l.id = t.location_id
             WHERE t.tenant_id = @TenantId
            """);

        var p = new DynamicParameters();
        p.Add("TenantId", TenantId);

        if (filter.ActiveOnly)
            sql.Append(" AND t.is_active = TRUE");

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            sql.Append(" AND (t.name ILIKE @Query OR t.description ILIKE @Query)");
            p.Add("Query", $"%{filter.Query}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            sql.Append(" AND t.category = @Category");
            p.Add("Category", filter.Category);
        }

        if (!string.IsNullOrWhiteSpace(filter.LocationPath))
        {
            sql.Append(" AND l.path <@ @LocationPath::ltree");
            p.Add("LocationPath", filter.LocationPath);
        }

        if (filter.AvailableOnly)
        {
            // Tool is available if it has no active (CheckedOut/Reserved) loan
            sql.Append("""
                 AND NOT EXISTS (
                     SELECT 1 FROM loans lo
                      WHERE lo.tool_id   = t.id
                        AND lo.tenant_id = t.tenant_id
                        AND lo.status    IN ('CheckedOut', 'Reserved')
                 )
                """);
        }

        sql.Append(" ORDER BY t.name");

        using var conn = OpenConnection();
        return await conn.QueryAsync<Tool, Location, Tool>(
            sql.ToString(),
            (tool, loc) => { tool.Location = loc; return tool; },
            p,
            splitOn: "id");
    }

    public async Task<Tool?> GetByIdAsync(Guid id)
    {
        using var conn = OpenConnection();
        var results = await conn.QueryAsync<Tool, Location, Tool>(
            """
            SELECT t.id, t.tenant_id, t.name, t.description, t.category,
                   t.owner_type, t.owner_user_id, t.condition, t.location_id,
                   t.qr_code, t.image_url, t.value_estimate, t.deposit_required,
                   t.is_active, t.created_utc, t.updated_utc,
                   l.id, l.tenant_id, l.name, l.code, l.path::text AS path,
                   l.parent_id, l.notes, l.created_utc, l.updated_utc
              FROM tools t
              LEFT JOIN locations l ON l.id = t.location_id
             WHERE t.tenant_id = @TenantId AND t.id = @Id
            """,
            (tool, loc) => { tool.Location = loc; return tool; },
            new { TenantId, Id = id },
            splitOn: "id");

        return results.FirstOrDefault();
    }

    public async Task<Tool?> GetByQrCodeAsync(string qrCode)
    {
        using var conn = OpenConnection();
        var results = await conn.QueryAsync<Tool, Location, Tool>(
            """
            SELECT t.id, t.tenant_id, t.name, t.description, t.category,
                   t.owner_type, t.owner_user_id, t.condition, t.location_id,
                   t.qr_code, t.image_url, t.value_estimate, t.deposit_required,
                   t.is_active, t.created_utc, t.updated_utc,
                   l.id, l.tenant_id, l.name, l.code, l.path::text AS path,
                   l.parent_id, l.notes, l.created_utc, l.updated_utc
              FROM tools t
              LEFT JOIN locations l ON l.id = t.location_id
             WHERE t.tenant_id = @TenantId AND t.qr_code = @QrCode
            """,
            (tool, loc) => { tool.Location = loc; return tool; },
            new { TenantId, QrCode = qrCode },
            splitOn: "id");

        return results.FirstOrDefault();
    }

    public async Task<Tool> CreateAsync(Tool tool)
    {
        tool.TenantId = TenantId;
        using var conn = OpenConnection();
        tool.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO tools
                (tenant_id, name, description, category, owner_type, owner_user_id,
                 condition, location_id, qr_code, image_url, value_estimate,
                 deposit_required, is_active, created_by)
            VALUES
                (@TenantId, @Name, @Description, @Category, @OwnerType::owner_type, @OwnerUserId,
                 @Condition::tool_condition, @LocationId, @QrCode, @ImageUrl, @ValueEstimate,
                 @DepositRequired, @IsActive, @CreatedBy)
            RETURNING id
            """, tool);
        return tool;
    }

    public async Task UpdateAsync(Tool tool)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE tools
               SET name             = @Name,
                   description      = @Description,
                   category         = @Category,
                   owner_type       = @OwnerType::owner_type,
                   owner_user_id    = @OwnerUserId,
                   condition        = @Condition::tool_condition,
                   location_id      = @LocationId,
                   image_url        = @ImageUrl,
                   value_estimate   = @ValueEstimate,
                   deposit_required = @DepositRequired,
                   updated_by       = @UpdatedBy
             WHERE tenant_id = @TenantId AND id = @Id
            """,
            new
            {
                TenantId,
                tool.Id, tool.Name, tool.Description, tool.Category,
                tool.OwnerType, tool.OwnerUserId, tool.Condition,
                tool.LocationId, tool.ImageUrl, tool.ValueEstimate,
                tool.DepositRequired, tool.UpdatedBy
            });
    }

    public async Task SetActiveAsync(Guid id, bool isActive, Guid updatedBy)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE tools SET is_active = @IsActive, updated_by = @UpdatedBy WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id, IsActive = isActive, UpdatedBy = updatedBy });
    }
}
