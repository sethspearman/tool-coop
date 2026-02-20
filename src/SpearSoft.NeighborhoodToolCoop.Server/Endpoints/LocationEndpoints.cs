using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this WebApplication app)
    {
        // GET /api/v1/locations  — full flat list (client builds the tree)
        app.MapGet("/api/v1/locations", async (ILocationRepository repo) =>
            Results.Ok(await repo.ListAllAsync())
        ).RequireAuthorization();

        // GET /api/v1/locations/{id}
        app.MapGet("/api/v1/locations/{id:guid}", async (Guid id, ILocationRepository repo) =>
        {
            var loc = await repo.GetByIdAsync(id);
            return loc is null ? Results.NotFound() : Results.Ok(loc);
        }).RequireAuthorization();

        // POST /api/v1/locations  — Manager+
        app.MapPost("/api/v1/locations", async (Location location, ILocationRepository repo) =>
        {
            var created = await repo.CreateAsync(location);
            return Results.Created($"/api/v1/locations/{created.Id}", created);
        }).RequireAuthorization("ManagerOrAbove");

        // PATCH /api/v1/locations/{id}  — rename/update notes
        app.MapPatch("/api/v1/locations/{id:guid}", async (
            Guid id, Location patch, ILocationRepository repo) =>
        {
            var loc = await repo.GetByIdAsync(id);
            if (loc is null) return Results.NotFound();
            loc.Name  = patch.Name;
            loc.Code  = patch.Code;
            loc.Notes = patch.Notes;
            await repo.UpdateAsync(loc);
            return Results.Ok(loc);
        }).RequireAuthorization("ManagerOrAbove");

        // POST /api/v1/locations/{id}/move  — subtree move
        app.MapPost("/api/v1/locations/{id:guid}/move", async (
            Guid id,
            MoveLoc body,
            ILocationRepository repo) =>
        {
            await repo.MoveAsync(id, body.NewParentId, body.NewPath);
            return Results.NoContent();
        }).RequireAuthorization("ManagerOrAbove");
    }
}

public record MoveLoc(Guid? NewParentId, string NewPath);
