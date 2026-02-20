namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class Notification
{
    public Guid   Id       { get; set; }
    public Guid   TenantId { get; set; }
    public Guid   UserId   { get; set; }
    public string Type     { get; set; } = string.Empty; // e.g. "LoanDueSoon", "Overdue"
    public string? Payload { get; set; }                 // JSONB serialized as string
    public string Status   { get; set; } = "Pending";   // notif_status enum
    public DateTimeOffset? SentUtc   { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
