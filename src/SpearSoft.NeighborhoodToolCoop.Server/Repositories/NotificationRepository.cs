using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class NotificationRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), INotificationRepository
{
    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.TenantId = TenantId;
        using var conn = OpenConnection();
        notification.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO notifications (tenant_id, user_id, type, payload, status)
            VALUES (@TenantId, @UserId, @Type, @Payload::jsonb, @Status::notif_status)
            RETURNING id
            """, notification);
        return notification;
    }

    public async Task<IEnumerable<Notification>> GetByUserAsync(Guid userId)
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Notification>(
            """
            SELECT * FROM notifications
             WHERE tenant_id = @TenantId AND user_id = @UserId
             ORDER BY created_utc DESC
             LIMIT 50
            """,
            new { TenantId, UserId = userId });
    }

    public async Task<IEnumerable<Notification>> GetPendingAsync()
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Notification>(
            "SELECT * FROM notifications WHERE tenant_id = @TenantId AND status = 'Pending'::notif_status",
            new { TenantId });
    }

    public async Task MarkSentAsync(Guid id)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE notifications
               SET status   = 'Sent'::notif_status,
                   sent_utc = NOW()
             WHERE tenant_id = @TenantId AND id = @Id
            """, new { TenantId, Id = id });
    }

    public async Task MarkFailedAsync(Guid id)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE notifications SET status = 'Failed'::notif_status WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }
}
