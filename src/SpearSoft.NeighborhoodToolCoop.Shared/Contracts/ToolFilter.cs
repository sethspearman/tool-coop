namespace SpearSoft.NeighborhoodToolCoop.Shared.Contracts;

/// <summary>Query parameters for the tool catalog list endpoint.</summary>
public class ToolFilter
{
    public string? Query        { get; set; }  // name/description search
    public string? Category     { get; set; }
    public bool    AvailableOnly { get; set; }
    public string? LocationPath  { get; set; } // ltree prefix, e.g. "SHED"
    public bool    ActiveOnly    { get; set; } = true;
}
