using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Services;

namespace SpearSoft.NeighborhoodToolCoop.Server.Middleware;

/// <summary>
/// Parses the tenant slug from /t/{slug}/... routes, looks it up in the DB,
/// and populates the scoped TenantContext. No-ops for other paths.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, DbConnectionFactory dbFactory)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/t/", StringComparison.OrdinalIgnoreCase))
        {
            // URL-based resolution: /t/{slug}/...
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var slug = parts[1];
                using var conn = dbFactory.Create();
                var row = await conn.QueryFirstOrDefaultAsync<TenantRow>(
                    "SELECT id, slug FROM tenants WHERE slug = @Slug AND status = 'Active'",
                    new { Slug = slug });

                if (row is not null)
                {
                    tenantContext.TenantId = row.Id;
                    tenantContext.Slug     = row.Slug;
                    tenantContext.IsResolved = true;
                }
            }
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            // Claim-based resolution for authenticated /api/v1/... calls.
            // UseAuthentication() runs before this middleware in Program.cs,
            // so context.User is already populated from the cookie.
            var tidClaim   = context.User.FindFirst("tenant_id")?.Value;
            var slugClaim  = context.User.FindFirst("tenant_slug")?.Value;

            if (Guid.TryParse(tidClaim, out var tenantId) && !string.IsNullOrEmpty(slugClaim))
            {
                tenantContext.TenantId  = tenantId;
                tenantContext.Slug      = slugClaim;
                tenantContext.IsResolved = true;
            }
        }

        await next(context);
    }

    private record TenantRow(Guid Id, string Slug);
}
