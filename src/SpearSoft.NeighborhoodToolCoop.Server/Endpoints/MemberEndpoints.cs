using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this WebApplication app)
    {
        // GET /api/v1/members  — Manager+
        app.MapGet("/api/v1/members", async (ITenantMemberRepository repo) =>
            Results.Ok(await repo.ListAsync())
        ).RequireAuthorization("ManagerOrAbove");

        // PATCH /api/v1/members/{userId}/role
        app.MapPatch("/api/v1/members/{userId:guid}/role", async (
            Guid userId, RoleRequest req, ITenantMemberRepository repo) =>
        {
            await repo.UpdateRoleAsync(userId, req.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // PATCH /api/v1/members/{userId}/status
        app.MapPatch("/api/v1/members/{userId:guid}/status", async (
            Guid userId, StatusRequest req, ITenantMemberRepository repo) =>
        {
            await repo.UpdateStatusAsync(userId, req.Value);
            return Results.NoContent();
        }).RequireAuthorization("ManagerOrAbove");
    }
}

public record RoleRequest(string Value);
public record StatusRequest(string Value);
