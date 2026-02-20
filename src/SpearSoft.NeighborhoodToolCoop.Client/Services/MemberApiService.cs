using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Net.Http.Json;

namespace SpearSoft.NeighborhoodToolCoop.Client.Services;

public class MemberApiService(HttpClient http)
{
    public async Task<List<TenantMember>> ListAsync() =>
        await http.GetFromJsonAsync<List<TenantMember>>("/api/v1/members") ?? [];

    public async Task<bool> UpdateRoleAsync(Guid userId, string role)
    {
        var resp = await http.PatchAsJsonAsync($"/api/v1/members/{userId}/role", new { Value = role });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateStatusAsync(Guid userId, string status)
    {
        var resp = await http.PatchAsJsonAsync($"/api/v1/members/{userId}/status", new { Value = status });
        return resp.IsSuccessStatusCode;
    }
}
