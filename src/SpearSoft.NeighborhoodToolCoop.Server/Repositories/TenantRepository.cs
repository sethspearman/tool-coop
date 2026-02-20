using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

/// <summary>Not tenant-scoped — used for tenant resolution and platform-admin operations.</summary>
public class TenantRepository(DbConnectionFactory dbFactory) : ITenantRepository
{
    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        using var conn = dbFactory.Create();
        return await conn.QueryFirstOrDefaultAsync<Tenant>(
            "SELECT * FROM tenants WHERE slug = @Slug",
            new { Slug = slug });
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        using var conn = dbFactory.Create();
        return await conn.QueryFirstOrDefaultAsync<Tenant>(
            "SELECT * FROM tenants WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        using var conn = dbFactory.Create();
        tenant.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO tenants (name, slug, plan, status, owner_user_id)
            VALUES (@Name, @Slug, @Plan::tenant_plan, @Status::tenant_status, @OwnerUserId)
            RETURNING id
            """, tenant);
        return tenant;
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        using var conn = dbFactory.Create();
        await conn.ExecuteAsync("""
            UPDATE tenants
               SET name                = @Name,
                   plan                = @Plan::tenant_plan,
                   status              = @Status::tenant_status,
                   owner_user_id       = @OwnerUserId,
                   billing_customer_id = @BillingCustomerId
             WHERE id = @Id
            """, tenant);
    }
}
