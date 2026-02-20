using Microsoft.AspNetCore.Components.Authorization;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SpearSoft.NeighborhoodToolCoop.Client.Auth;

/// <summary>
/// Determines auth state for the Blazor WASM app by calling /api/me on the
/// server. The server-issued cookie is sent automatically (same origin).
/// </summary>
public class ServerAuthenticationStateProvider(HttpClient http) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var user = await http.GetFromJsonAsync<CurrentUserDto>("/api/me");
            if (user is null) return Anonymous;

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name,           user.DisplayName),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role),
                new Claim("tenant_id",               user.TenantId.ToString()),
                new Claim("tenant_slug",             user.TenantSlug),
                new Claim("member_status",           user.Status),
            ],
            authenticationType: "ServerCookie");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            // /api/me returned 401 or network error — treat as anonymous
            return Anonymous;
        }
    }
}
