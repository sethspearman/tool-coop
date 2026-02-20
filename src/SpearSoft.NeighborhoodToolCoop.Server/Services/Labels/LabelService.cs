using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services.Labels;
using System.Text;

namespace SpearSoft.NeighborhoodToolCoop.Server.Services;

/// <summary>
/// Orchestrates label generation for tools and locations.
/// Scoped per request — uses the request's TenantContext via the injected repos.
/// </summary>
public class LabelService(
    IToolRepository     tools,
    ILocationRepository locations,
    TenantContext       tenantContext,
    QrCodeGenerator     qr,
    IConfiguration      config)
{
    private string BaseUrl =>
        config["App:BaseUrl"]?.TrimEnd('/') ?? "https://toolcoop.app";

    private string TenantSlug => tenantContext.Slug;

    // ── Tool labels ─────────────────────────────────────────────────────────

    public async Task<LabelResult> GenerateToolLabelAsync(Guid toolId)
    {
        var tool = await tools.GetByIdAsync(toolId)
            ?? throw new KeyNotFoundException($"Tool {toolId} not found.");

        var qrPayload  = $"{BaseUrl}/t/{TenantSlug}/tool/{tool.Id}?a=scan";
        var qrPng      = qr.GeneratePng(qrPayload);
        var svg        = SvgLabelBuilder.BuildToolLabel(tool, qrPng, TenantSlug);

        return new LabelResult(
            ContentType: "image/svg+xml",
            Content:     Encoding.UTF8.GetBytes(svg),
            FileName:    $"label-{tool.QrCode}.svg");
    }

    public async Task<LabelResult> GenerateBatchToolLabelsAsync(IEnumerable<Guid> toolIds)
    {
        var svgs = new List<string>();

        foreach (var id in toolIds)
        {
            var tool = await tools.GetByIdAsync(id);
            if (tool is null) continue;

            var qrPayload = $"{BaseUrl}/t/{TenantSlug}/tool/{tool.Id}?a=scan";
            var qrPng     = qr.GeneratePng(qrPayload);
            svgs.Add(SvgLabelBuilder.BuildToolLabel(tool, qrPng, TenantSlug));
        }

        var html = SvgLabelBuilder.BuildBatchHtml(svgs, $"{TenantSlug} — Tool Labels");

        return new LabelResult(
            ContentType: "text/html",
            Content:     Encoding.UTF8.GetBytes(html),
            FileName:    $"labels-{TenantSlug}-tools.html");
    }

    // ── Location labels ─────────────────────────────────────────────────────

    public async Task<LabelResult> GenerateLocationLabelAsync(Guid locationId)
    {
        var location = await locations.GetByIdAsync(locationId)
            ?? throw new KeyNotFoundException($"Location {locationId} not found.");

        // QR payload uses the location code so it works with the native-camera scan flow
        var qrPayload = $"{BaseUrl}/t/{TenantSlug}/location/{location.Code}?a=scan";
        var qrPng     = qr.GeneratePng(qrPayload);
        var svg       = SvgLabelBuilder.BuildLocationLabel(location, qrPng, TenantSlug);

        return new LabelResult(
            ContentType: "image/svg+xml",
            Content:     Encoding.UTF8.GetBytes(svg),
            FileName:    $"label-{location.Code}.svg");
    }

    public async Task<LabelResult> GenerateBatchLocationLabelsAsync(IEnumerable<Guid> locationIds)
    {
        var svgs = new List<string>();

        foreach (var id in locationIds)
        {
            var location = await locations.GetByIdAsync(id);
            if (location is null) continue;

            var qrPayload = $"{BaseUrl}/t/{TenantSlug}/location/{location.Code}?a=scan";
            var qrPng     = qr.GeneratePng(qrPayload);
            svgs.Add(SvgLabelBuilder.BuildLocationLabel(location, qrPng, TenantSlug));
        }

        var html = SvgLabelBuilder.BuildBatchHtml(svgs, $"{TenantSlug} — Location Labels");

        return new LabelResult(
            ContentType: "text/html",
            Content:     Encoding.UTF8.GetBytes(html),
            FileName:    $"labels-{TenantSlug}-locations.html");
    }
}

/// <summary>Carries the generated label content back to the endpoint.</summary>
public record LabelResult(string ContentType, byte[] Content, string FileName);
