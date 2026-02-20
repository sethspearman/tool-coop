using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class TenantMemberRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), ITenantMemberRepository
{
    public async Task<TenantMember?> GetAsync(Guid userId)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<TenantMember>(
            """
            SELECT tu.*, u.display_name, u.email, u.avatar_url, u.phone
              FROM tenant_users tu
              JOIN users u ON u.id = tu.user_id
             WHERE tu.tenant_id = @TenantId AND tu.user_id = @UserId
            """,
            new { TenantId, UserId = userId });
    }

    public async Task<IEnumerable<TenantMember>> ListAsync()
    {
        using var conn = OpenConnection();
        // Multi-map: TenantMember + User
        return await conn.QueryAsync<TenantMember, User, TenantMember>(
            """
            SELECT tu.tenant_id, tu.user_id, tu.role, tu.status, tu.joined_utc, tu.updated_utc,
                   u.id, u.tenant_id, u.display_name, u.email, u.phone, u.avatar_url, u.google_subject,
                   u.created_utc, u.updated_utc
              FROM tenant_users tu
              JOIN users u ON u.id = tu.user_id
             WHERE tu.tenant_id = @TenantId
             ORDER BY tu.joined_utc
            """,
            (member, user) => { member.User = user; return member; },
            new { TenantId },
            splitOn: "id");
    }

    public async Task UpsertAsync(TenantMember member)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            INSERT INTO tenant_users (tenant_id, user_id, role, status)
            VALUES (@TenantId, @UserId, @Role::user_role, @Status::member_status)
            ON CONFLICT (tenant_id, user_id) DO UPDATE
               SET role   = EXCLUDED.role,
                   status = EXCLUDED.status
            """,
            new { TenantId, member.UserId, member.Role, member.Status });
    }

    public async Task UpdateRoleAsync(Guid userId, string role)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE tenant_users SET role = @Role::user_role WHERE tenant_id = @TenantId AND user_id = @UserId",
            new { TenantId, UserId = userId, Role = role });
    }

    public async Task UpdateStatusAsync(Guid userId, string status)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE tenant_users SET status = @Status::member_status WHERE tenant_id = @TenantId AND user_id = @UserId",
            new { TenantId, UserId = userId, Status = status });
    }
}
