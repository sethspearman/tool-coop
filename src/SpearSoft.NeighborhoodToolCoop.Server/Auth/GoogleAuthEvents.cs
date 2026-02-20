using Dapper;
using Microsoft.AspNetCore.Authentication.OAuth;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using System.Security.Claims;

namespace SpearSoft.NeighborhoodToolCoop.Server.Auth;

/// <summary>
/// Fired by the Google OIDC handler after a successful token exchange.
/// Looks up (or creates) the app user for this (tenant, google_subject) pair
/// and adds app-specific claims to the principal before the auth cookie is issued.
/// </summary>
public static class GoogleAuthEvents
{
    public static async Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var dbFactory = context.HttpContext.RequestServices
            .GetRequiredService<DbConnectionFactory>();

        // Google "sub" claim uniquely identifies the user across sessions
        var googleSubject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google subject claim is missing.");

        var email       = context.Principal?.FindFirstValue(ClaimTypes.Email);
        var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name);
        var avatarUrl   = context.Principal?.FindFirstValue("picture");

        // Tenant slug was stored in AuthenticationProperties.Items when the
        // challenge was issued from /t/{slug}/auth/login
        if (!context.Properties.Items.TryGetValue("tenantSlug", out var tenantSlug) || tenantSlug is null)
            throw new InvalidOperationException("Tenant slug is missing from OAuth state.");

        using var conn = dbFactory.Create();

        // Resolve tenant
        var tenantId = await conn.ExecuteScalarAsync<Guid?>(
            "SELECT id FROM tenants WHERE slug = @Slug AND status = 'Active'",
            new { Slug = tenantSlug });

        if (tenantId is null)
            throw new InvalidOperationException($"Tenant '{tenantSlug}' not found or is inactive.");

        // Look up existing user
        var userId = await conn.ExecuteScalarAsync<Guid?>(
            "SELECT id FROM users WHERE tenant_id = @TenantId AND google_subject = @GoogleSubject",
            new { TenantId = tenantId, GoogleSubject = googleSubject });

        string role;
        string memberStatus;

        if (userId is null)
        {
            // First sign-in for this tenant — create user + membership record
            userId = Guid.NewGuid();

            await conn.ExecuteAsync("""
                INSERT INTO users (id, tenant_id, display_name, email, avatar_url, google_subject)
                VALUES (@Id, @TenantId, @DisplayName, @Email, @AvatarUrl, @GoogleSubject)
                """,
                new
                {
                    Id = userId,
                    TenantId = tenantId,
                    DisplayName = displayName,
                    Email = email,
                    AvatarUrl = avatarUrl,
                    GoogleSubject = googleSubject
                });

            await conn.ExecuteAsync("""
                INSERT INTO tenant_users (tenant_id, user_id, role, status)
                VALUES (@TenantId, @UserId, 'Member', 'Pending')
                """,
                new { TenantId = tenantId, UserId = userId });

            role = "Member";
            memberStatus = "Pending";
        }
        else
        {
            // Existing user — fetch current role and membership status
            var membership = await conn.QueryFirstOrDefaultAsync<MembershipRow>(
                "SELECT role, status FROM tenant_users WHERE tenant_id = @TenantId AND user_id = @UserId",
                new { TenantId = tenantId, UserId = userId });

            role = membership?.Role ?? "Member";
            memberStatus = membership?.Status ?? "Active";
        }

        // Add app-specific claims to the principal before the cookie is written
        var identity = (ClaimsIdentity)context.Principal!.Identity!;
        identity.AddClaim(new Claim("app_user_id",   userId.ToString()!));
        identity.AddClaim(new Claim("tenant_id",     tenantId.ToString()!));
        identity.AddClaim(new Claim("tenant_slug",   tenantSlug));
        identity.AddClaim(new Claim("member_status", memberStatus));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    private record MembershipRow(string Role, string Status);
}
