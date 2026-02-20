using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using System.Security.Claims;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // ------------------------------------------------------------------
        // GET /t/{tenantSlug}/auth/login?returnUrl=...
        //   Initiates the Google OAuth challenge.
        //   The tenant slug is stored in AuthenticationProperties so it
        //   survives the round-trip through Google and is available in
        //   GoogleAuthEvents.OnCreatingTicket.
        // ------------------------------------------------------------------
        app.MapGet("/t/{tenantSlug}/auth/login", (string tenantSlug, string? returnUrl, HttpContext context) =>
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? $"/t/{tenantSlug}",
                Items = { ["tenantSlug"] = tenantSlug }
            };
            return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
        });

        // ------------------------------------------------------------------
        // POST /t/{tenantSlug}/auth/logout
        //   Signs out and redirects back to the tenant root.
        // ------------------------------------------------------------------
        app.MapPost("/t/{tenantSlug}/auth/logout", async (string tenantSlug, HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect($"/t/{tenantSlug}");
        });

        // ------------------------------------------------------------------
        // GET /api/me
        //   Returns the current user's profile for the Blazor WASM client to
        //   bootstrap its AuthenticationState. Returns 401 if not signed in.
        // ------------------------------------------------------------------
        app.MapGet("/api/me", (HttpContext context) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var dto = new CurrentUserDto
            {
                UserId      = Guid.Parse(context.User.FindFirstValue("app_user_id")!),
                TenantId    = Guid.Parse(context.User.FindFirstValue("tenant_id")!),
                TenantSlug  = context.User.FindFirstValue("tenant_slug")!,
                DisplayName = context.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Email       = context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                AvatarUrl   = context.User.FindFirstValue("picture"),
                Role        = context.User.FindFirstValue(ClaimTypes.Role) ?? "Member",
                Status      = context.User.FindFirstValue("member_status") ?? "Active"
            };

            return Results.Ok(dto);
        });
    }
}
