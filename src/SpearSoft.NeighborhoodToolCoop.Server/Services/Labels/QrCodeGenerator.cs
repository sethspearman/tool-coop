using QRCoder;

namespace SpearSoft.NeighborhoodToolCoop.Server.Services.Labels;

/// <summary>
/// Wraps QRCoder to produce QR code output in PNG or SVG format.
/// Registered as a singleton — QRCodeGenerator itself is stateless.
/// </summary>
public class QrCodeGenerator
{
    /// <summary>
    /// Returns raw PNG bytes for the given payload.
    /// pixelsPerModule controls the physical size: 4 → ~80px at typical label resolution.
    /// </summary>
    public byte[] GeneratePng(string payload, int pixelsPerModule = 4)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var code = new PngByteQRCode(data);
        return code.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Returns a standalone SVG string for the given payload.
    /// Useful when embedding the QR inside a larger SVG via foreignObject.
    /// </summary>
    public string GenerateSvg(string payload, int pixelsPerModule = 3)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var code = new SvgQRCode(data);
        return code.GetGraphic(pixelsPerModule, "#000000", "#FFFFFF", drawQuietZones: true);
    }
}
