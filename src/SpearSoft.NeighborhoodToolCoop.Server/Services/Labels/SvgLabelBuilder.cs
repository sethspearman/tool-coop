using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Text;

namespace SpearSoft.NeighborhoodToolCoop.Server.Services.Labels;

/// <summary>
/// Builds 2" × 1" thermal label SVGs.
///
/// Coordinate space: viewBox="0 0 192 96" (96 dpi — 2in × 1in).
/// Layout:
///   Left  0–87:  QR code (80×80, centred vertically)
///   Right 90–192: text block (name, slug ▸ code, location, condition)
///
/// The QR PNG is embedded as a base64 data URL so the SVG is fully self-contained
/// and renders correctly both in browsers and in print pipelines.
/// </summary>
public static class SvgLabelBuilder
{
    private const int W = 192;
    private const int H = 96;
    private const int QrSize = 80;
    private const int QrX = 4;
    private const int QrY = 8;
    private const int TextX = 92;

    // ── Tool label ──────────────────────────────────────────────────────────

    public static string BuildToolLabel(Tool tool, byte[] qrPngBytes, string tenantSlug)
    {
        var qrDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrPngBytes)}";
        var name      = Truncate(tool.Name, 20);
        var codeLine  = $"{tenantSlug} \u25b8 {tool.QrCode}";   // ▸
        var locLine   = tool.Location is not null
            ? LocationShortPath(tool.Location.Path)
            : "No location";
        var condLine  = $"Condition: {tool.Condition}";

        return Svg(qrDataUrl, new[]
        {
            (TextX, 22, 11, "bold", "#111", name),
            (TextX, 38,  9, "normal", "#333", codeLine),
            (TextX, 52,  9, "normal", "#555", locLine),
            (TextX, 66,  8, "normal", "#888", condLine),
        },
        footerLeft: tool.QrCode);
    }

    // ── Location label ──────────────────────────────────────────────────────

    public static string BuildLocationLabel(Location location, byte[] qrPngBytes, string tenantSlug)
    {
        var qrDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrPngBytes)}";
        var name      = Truncate(location.Name, 18);
        var codeLine  = location.Code;
        var pathLine  = LocationShortPath(location.Path);
        var slugLine  = tenantSlug;

        return Svg(qrDataUrl, new[]
        {
            (TextX, 24, 13, "bold",   "#111", name),
            (TextX, 42, 11, "normal", "#333", codeLine),
            (TextX, 57,  9, "normal", "#666", pathLine),
            (TextX, 71,  8, "normal", "#999", slugLine),
        },
        footerLeft: location.Code);
    }

    // ── Batch HTML wrapper ──────────────────────────────────────────────────

    /// <summary>
    /// Wraps a list of label SVGs in a printable HTML page.
    /// CSS @page is set to 2in × 1in so each label fills exactly one thermal label.
    /// </summary>
    public static string BuildBatchHtml(IEnumerable<string> labelSvgs, string title)
    {
        var sb = new StringBuilder();
        var encodedTitle = System.Web.HttpUtility.HtmlEncode(title);
        sb.AppendLine($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <title>{{encodedTitle}}</title>
              <style>
                @page { size: 2in 1in; margin: 0; }
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { background: white; }
                .label {
                  width: 2in; height: 1in;
                  overflow: hidden;
                  page-break-after: always;
                }
                .label:last-child { page-break-after: auto; }
                svg { display: block; }
              </style>
            </head>
            <body>
            """);

        foreach (var svg in labelSvgs)
        {
            sb.AppendLine($"<div class=\"label\">{svg}</div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    // ── SVG template ────────────────────────────────────────────────────────

    private static string Svg(
        string qrDataUrl,
        IEnumerable<(int x, int y, int fontSize, string fontWeight, string fill, string text)> lines,
        string footerLeft)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="2in" height="1in" viewBox="0 0 {W} {H}"
                 style="font-family: sans-serif; background: white;">
              <rect x="0" y="0" width="{W}" height="{H}" fill="white" stroke="#ddd" stroke-width="0.5"/>
              <image x="{QrX}" y="{QrY}" width="{QrSize}" height="{QrSize}" href="{qrDataUrl}"/>
              <line x1="88" y1="4" x2="88" y2="92" stroke="#eee" stroke-width="0.5"/>
            """);

        foreach (var (x, y, fs, fw, fill, text) in lines)
        {
            var escaped = System.Web.HttpUtility.HtmlEncode(text);
            sb.AppendLine(
                $"""  <text x="{x}" y="{y}" font-size="{fs}" font-weight="{fw}" fill="{fill}">{escaped}</text>""");
        }

        var escapedFooter = System.Web.HttpUtility.HtmlEncode(footerLeft);
        sb.AppendLine($"""  <text x="4" y="94" font-size="6" fill="#bbb" font-family="monospace">{escapedFooter}</text>""");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..(maxChars - 1)] + "\u2026"; // …

    /// <summary>
    /// Shows the last two ltree segments with a "/" separator.
    /// "SHED.Shelf1.BinA" → "Shelf1 / BinA"
    /// </summary>
    private static string LocationShortPath(string ltreePath)
    {
        var parts = ltreePath.Split('.');
        return parts.Length switch
        {
            0 => ltreePath,
            1 => parts[0],
            _ => string.Join(" / ", parts[^2..])
        };
    }
}
