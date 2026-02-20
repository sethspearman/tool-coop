using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class LocationRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), ILocationRepository
{
    // path is cast to text in every SELECT so Dapper maps it as string without
    // needing a custom Npgsql ltree type handler.
    private const string SelectCols =
        "id, tenant_id, name, code, path::text AS path, parent_id, notes, created_utc, updated_utc";

    public async Task<Location?> GetByIdAsync(Guid id)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Location>(
            $"SELECT {SelectCols} FROM locations WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }

    public async Task<IEnumerable<Location>> ListAllAsync()
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Location>(
            $"SELECT {SelectCols} FROM locations WHERE tenant_id = @TenantId ORDER BY path",
            new { TenantId });
    }

    public async Task<IEnumerable<Location>> GetChildrenAsync(Guid parentId)
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Location>(
            $"""
            SELECT {SelectCols} FROM locations
             WHERE tenant_id = @TenantId AND parent_id = @ParentId
             ORDER BY name
            """,
            new { TenantId, ParentId = parentId });
    }

    public async Task<IEnumerable<Location>> GetSubtreeAsync(string rootPath)
    {
        using var conn = OpenConnection();
        // <@ operator: "is descendant of or equal to"
        return await conn.QueryAsync<Location>(
            $"""
            SELECT {SelectCols} FROM locations
             WHERE tenant_id = @TenantId
               AND path <@ @RootPath::ltree
             ORDER BY path
            """,
            new { TenantId, RootPath = rootPath });
    }

    public async Task<Location> CreateAsync(Location location)
    {
        location.TenantId = TenantId;
        using var conn = OpenConnection();
        location.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO locations (tenant_id, name, code, path, parent_id, notes)
            VALUES (@TenantId, @Name, @Code, @Path::ltree, @ParentId, @Notes)
            RETURNING id
            """, location);
        return location;
    }

    public async Task UpdateAsync(Location location)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE locations
               SET name  = @Name,
                   code  = @Code,
                   notes = @Notes
             WHERE tenant_id = @TenantId AND id = @Id
            """,
            new { TenantId, location.Id, location.Name, location.Code, location.Notes });
    }

    public async Task MoveAsync(Guid id, Guid? newParentId, string newPath)
    {
        using var conn = OpenConnection();

        // Fetch old path first so we can rewrite all descendant paths
        var oldPath = await conn.ExecuteScalarAsync<string>(
            "SELECT path::text FROM locations WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });

        if (oldPath is null) return;

        // Update all descendants: replace the old prefix with the new one
        await conn.ExecuteAsync("""
            UPDATE locations
               SET path      = (@NewPath::ltree || subpath(path, nlevel(@OldPath::ltree)))::ltree,
                   parent_id = CASE WHEN id = @Id THEN @NewParentId ELSE parent_id END
             WHERE tenant_id = @TenantId
               AND path <@ @OldPath::ltree
            """,
            new { TenantId, Id = id, OldPath = oldPath, NewPath = newPath, NewParentId = newParentId });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "DELETE FROM locations WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }
}
