using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class LabelEndpoints
{
    public static void MapLabelEndpoints(this WebApplication app)
    {
        // ── Tool labels ────────────────────────────────────────────────────

        // GET /t/{slug}/api/v1/tools/{toolId}/labels
        // Returns a single 2"×1" tool label as SVG (self-contained, printable).
        app.MapGet("/t/{tenantSlug}/api/v1/tools/{toolId}/labels",
            async (string tenantSlug, Guid toolId, LabelService labelSvc) =>
            {
                var result = await labelSvc.GenerateToolLabelAsync(toolId);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequireAuthorization();

        // POST /t/{slug}/api/v1/tools/labels/batch
        // Body: { "ids": ["guid1", "guid2", ...] }
        // Returns a printable HTML page — open in browser and Ctrl+P to your label printer.
        app.MapPost("/t/{tenantSlug}/api/v1/tools/labels/batch",
            async (string tenantSlug, BatchLabelRequest request, LabelService labelSvc) =>
            {
                var result = await labelSvc.GenerateBatchToolLabelsAsync(request.Ids);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequireAuthorization();

        // ── Location labels ────────────────────────────────────────────────

        // GET /t/{slug}/api/v1/locations/{locationId}/labels
        // Returns a single 2"×1" location shelf/bin label as SVG.
        app.MapGet("/t/{tenantSlug}/api/v1/locations/{locationId}/labels",
            async (string tenantSlug, Guid locationId, LabelService labelSvc) =>
            {
                var result = await labelSvc.GenerateLocationLabelAsync(locationId);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequireAuthorization();

        // POST /t/{slug}/api/v1/locations/labels/batch
        app.MapPost("/t/{tenantSlug}/api/v1/locations/labels/batch",
            async (string tenantSlug, BatchLabelRequest request, LabelService labelSvc) =>
            {
                var result = await labelSvc.GenerateBatchLocationLabelsAsync(request.Ids);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequireAuthorization();
    }
}
