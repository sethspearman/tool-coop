namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class Reservation
{
    public Guid   Id          { get; set; }
    public Guid   TenantId    { get; set; }
    public Guid   ToolId      { get; set; }
    public Guid   UserId      { get; set; }
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd   { get; set; }
    public string Status      { get; set; } = "Pending"; // Pending | Approved | Canceled
    public DateTimeOffset CreatedUtc  { get; set; }
    public DateTimeOffset UpdatedUtc  { get; set; }

    // Populated by join queries
    public Tool? Tool { get; set; }
    public User? User { get; set; }
}
