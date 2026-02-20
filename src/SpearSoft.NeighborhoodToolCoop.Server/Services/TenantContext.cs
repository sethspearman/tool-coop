namespace SpearSoft.NeighborhoodToolCoop.Server.Services;

/// <summary>
/// Scoped service that carries the resolved tenant for the current request.
/// Populated by TenantResolutionMiddleware for any /t/{slug}/... route.
/// </summary>
public class TenantContext
{
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}
