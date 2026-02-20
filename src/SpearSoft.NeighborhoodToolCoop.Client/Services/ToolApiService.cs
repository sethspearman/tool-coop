using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Net.Http.Json;

namespace SpearSoft.NeighborhoodToolCoop.Client.Services;

public class ToolApiService(HttpClient http)
{
    public async Task<List<Tool>> ListAsync(ToolFilter? filter = null)
    {
        var url = "/api/v1/tools";
        if (filter is not null)
        {
            var qs = BuildQuery(filter);
            if (qs.Length > 0) url += "?" + qs;
        }
        return await http.GetFromJsonAsync<List<Tool>>(url) ?? [];
    }

    public async Task<Tool?> GetByIdAsync(Guid id) =>
        await http.GetFromJsonAsync<Tool>($"/api/v1/tools/{id}");

    public async Task<Tool?> GetByQrCodeAsync(string code) =>
        await http.GetFromJsonAsync<Tool>($"/api/v1/tools/qr/{Uri.EscapeDataString(code)}");

    public async Task<Tool?> CreateAsync(CreateToolRequest req)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/tools", req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Tool>();
    }

    private static string BuildQuery(ToolFilter f)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Query))
            parts.Add($"query={Uri.EscapeDataString(f.Query)}");
        if (!string.IsNullOrWhiteSpace(f.Category))
            parts.Add($"category={Uri.EscapeDataString(f.Category)}");
        if (f.AvailableOnly)
            parts.Add("availableOnly=true");
        if (!string.IsNullOrWhiteSpace(f.LocationPath))
            parts.Add($"locationPath={Uri.EscapeDataString(f.LocationPath)}");
        if (!f.ActiveOnly)  // only send when deviating from server default (true)
            parts.Add("activeOnly=false");
        return string.Join("&", parts);
    }
}
