using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class UserRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }

    public async Task<User?> GetByGoogleSubjectAsync(string googleSubject)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE tenant_id = @TenantId AND google_subject = @GoogleSubject",
            new { TenantId, GoogleSubject = googleSubject });
    }

    public async Task<User> CreateAsync(User user)
    {
        user.TenantId = TenantId;
        using var conn = OpenConnection();
        user.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO users (tenant_id, display_name, email, phone, avatar_url, google_subject)
            VALUES (@TenantId, @DisplayName, @Email, @Phone, @AvatarUrl, @GoogleSubject)
            RETURNING id
            """, user);
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE users
               SET display_name = @DisplayName,
                   email        = @Email,
                   phone        = @Phone,
                   avatar_url   = @AvatarUrl
             WHERE tenant_id = @TenantId
               AND id        = @Id
            """, new { TenantId, user.Id, user.DisplayName, user.Email, user.Phone, user.AvatarUrl });
    }
}
