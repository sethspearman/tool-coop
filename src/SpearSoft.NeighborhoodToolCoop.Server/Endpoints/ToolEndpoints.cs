using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Security.Claims;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class ToolEndpoints
{
    public static void MapToolEndpoints(this WebApplication app)
    {
        // GET /api/v1/tools?query=&category=&availableOnly=&locationPath=&activeOnly=
        app.MapGet("/api/v1/tools", async (
            IToolRepository repo,
            string? query,
            string? category,
            bool?   availableOnly,
            string? locationPath,
            bool?   activeOnly) =>
        {
            var filter = new ToolFilter
            {
                Query        = query,
                Category     = category,
                AvailableOnly = availableOnly ?? false,
                LocationPath = locationPath,
                ActiveOnly   = activeOnly ?? true
            };
            return Results.Ok(await repo.ListAsync(filter));
        }).RequireAuthorization();

        // GET /api/v1/tools/{id}  — includes attributes
        app.MapGet("/api/v1/tools/{id:guid}", async (
            Guid id,
            IToolRepository     toolRepo,
            IToolAttributeRepository attrRepo) =>
        {
            var tool = await toolRepo.GetByIdAsync(id);
            if (tool is null) return Results.NotFound();
            tool.Attributes = (await attrRepo.GetByToolAsync(id)).ToList();
            return Results.Ok(tool);
        }).RequireAuthorization();

        // GET /api/v1/tools/qr/{code}  — QR scan lookup
        app.MapGet("/api/v1/tools/qr/{code}", async (
            string code,
            IToolRepository     toolRepo,
            IToolAttributeRepository attrRepo) =>
        {
            var tool = await toolRepo.GetByQrCodeAsync(code);
            if (tool is null) return Results.NotFound();
            tool.Attributes = (await attrRepo.GetByToolAsync(tool.Id)).ToList();
            return Results.Ok(tool);
        }).RequireAuthorization();

        // POST /api/v1/tools  — Manager+ only
        app.MapPost("/api/v1/tools", async (
            CreateToolRequest   req,
            IToolRepository     repo,
            HttpContext         ctx) =>
        {
            var actorId = GetUserId(ctx);
            var tool = new Tool
            {
                Name            = req.Name,
                Description     = req.Description,
                Category        = req.Category,
                OwnerType       = req.OwnerType,
                OwnerUserId     = req.OwnerUserId,
                Condition       = req.Condition,
                LocationId      = req.LocationId,
                QrCode          = req.QrCode,
                ImageUrl        = req.ImageUrl,
                ValueEstimate   = req.ValueEstimate,
                DepositRequired = req.DepositRequired,
                IsActive        = true,
                CreatedBy       = actorId
            };
            var created = await repo.CreateAsync(tool);
            return Results.Created($"/api/v1/tools/{created.Id}", created);
        }).RequireAuthorization("ManagerOrAbove");

        // PATCH /api/v1/tools/{id}  — Manager+ only
        app.MapPatch("/api/v1/tools/{id:guid}", async (
            Guid id,
            CreateToolRequest req,
            IToolRepository   repo,
            HttpContext        ctx) =>
        {
            var tool = await repo.GetByIdAsync(id);
            if (tool is null) return Results.NotFound();

            tool.Name            = req.Name;
            tool.Description     = req.Description;
            tool.Category        = req.Category;
            tool.OwnerType       = req.OwnerType;
            tool.OwnerUserId     = req.OwnerUserId;
            tool.Condition       = req.Condition;
            tool.LocationId      = req.LocationId;
            tool.ImageUrl        = req.ImageUrl;
            tool.ValueEstimate   = req.ValueEstimate;
            tool.DepositRequired = req.DepositRequired;
            tool.UpdatedBy       = GetUserId(ctx);

            await repo.UpdateAsync(tool);
            return Results.Ok(tool);
        }).RequireAuthorization("ManagerOrAbove");
    }

    private static Guid? GetUserId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirstValue("app_user_id"), out var id) ? id : null;
}
