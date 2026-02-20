using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class ReservationRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), IReservationRepository
{
    public async Task<Reservation?> GetByIdAsync(Guid id)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Reservation>(
            "SELECT * FROM reservations WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }

    public async Task<IEnumerable<Reservation>> GetByToolAsync(Guid toolId)
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Reservation>(
            """
            SELECT * FROM reservations
             WHERE tenant_id = @TenantId AND tool_id = @ToolId
               AND status NOT IN ('Canceled')
             ORDER BY window_start
            """,
            new { TenantId, ToolId = toolId });
    }

    public async Task<IEnumerable<Reservation>> GetByUserAsync(Guid userId)
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Reservation>(
            """
            SELECT * FROM reservations
             WHERE tenant_id = @TenantId AND user_id = @UserId
             ORDER BY window_start DESC
            """,
            new { TenantId, UserId = userId });
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        reservation.TenantId = TenantId;
        using var conn = OpenConnection();
        reservation.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO reservations (tenant_id, tool_id, user_id, window_start, window_end, status)
            VALUES (@TenantId, @ToolId, @UserId, @WindowStart, @WindowEnd, @Status)
            RETURNING id
            """, reservation);
        return reservation;
    }

    public async Task ApproveAsync(Guid id)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET status = 'Approved' WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }

    public async Task CancelAsync(Guid id)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET status = 'Canceled' WHERE tenant_id = @TenantId AND id = @Id",
            new { TenantId, Id = id });
    }
}
