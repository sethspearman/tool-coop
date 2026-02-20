namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

/// <summary>Maps to tool_attributes. Key/value extensibility per tool.</summary>
public class ToolAttribute
{
    public Guid   Id       { get; set; }
    public Guid   TenantId { get; set; }
    public Guid   ToolId   { get; set; }
    public string Key      { get; set; } = string.Empty;
    public string Value    { get; set; } = string.Empty;
}
