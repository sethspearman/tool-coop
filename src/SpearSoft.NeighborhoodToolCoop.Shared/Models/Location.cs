namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

/// <summary>
/// Maps to locations. path is an ltree value stored as a string
/// (cast to text in all SELECT queries for Dapper compatibility).
/// </summary>
public class Location
{
    public Guid   Id        { get; set; }
    public Guid   TenantId  { get; set; }
    public string Name      { get; set; } = string.Empty;  // human-readable, e.g. "Shelf 1"
    public string Code      { get; set; } = string.Empty;  // short code, e.g. "SH1"
    public string Path      { get; set; } = string.Empty;  // ltree path, e.g. "SHED.Shelf1"
    public Guid?  ParentId  { get; set; }
    public string? Notes    { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
