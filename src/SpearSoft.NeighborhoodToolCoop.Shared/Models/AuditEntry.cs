namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

/// <summary>
/// Maps to audit_log. Append-only; BigSerial PK for ordering.
/// No tenant FK — log survives tenant deletion.
/// </summary>
public class AuditEntry
{
    public long   Id         { get; set; }
    public Guid   TenantId   { get; set; }
    public Guid?  ActorId    { get; set; }
    public string Action     { get; set; } = string.Empty; // e.g. "Tool.Create"
    public string EntityType { get; set; } = string.Empty; // e.g. "Tool"
    public Guid?  EntityId   { get; set; }
    public string? OldValues { get; set; } // JSONB as string
    public string? NewValues { get; set; } // JSONB as string
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
