using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Net.Http.Json;

namespace SpearSoft.NeighborhoodToolCoop.Client.Services;

public class LocationApiService(HttpClient http)
{
    public async Task<List<Location>> ListAllAsync() =>
        await http.GetFromJsonAsync<List<Location>>("/api/v1/locations") ?? [];

    public async Task<Location?> GetByIdAsync(Guid id) =>
        await http.GetFromJsonAsync<Location>($"/api/v1/locations/{id}");

    /// <summary>
    /// Builds a parent→children tree from the flat list returned by the API.
    /// Sorts by ltree path so parents always appear before children.
    /// </summary>
    public static List<LocationNode> BuildTree(List<Location> flat)
    {
        var map = flat
            .OrderBy(l => l.Path)
            .ToDictionary(l => l.Id, l => new LocationNode(l));

        var roots = new List<LocationNode>();
        foreach (var node in map.Values)
        {
            if (node.Location.ParentId.HasValue &&
                map.TryGetValue(node.Location.ParentId.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }
        return roots;
    }
}

public class LocationNode(Location location)
{
    public Location          Location { get; } = location;
    public List<LocationNode> Children { get; } = [];
    public int Depth => Location.Path.Count(c => c == '.');
}
