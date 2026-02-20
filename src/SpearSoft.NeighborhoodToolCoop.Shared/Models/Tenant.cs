namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class Tenant
{
    public Guid   Id                  { get; set; }
    public string Name                { get; set; } = string.Empty;
    public string Slug                { get; set; } = string.Empty;
    public string Plan                { get; set; } = "Community";  // tenant_plan enum
    public string Status              { get; set; } = "Active";     // tenant_status enum
    public Guid?  OwnerUserId         { get; set; }
    public string? BillingCustomerId  { get; set; }
    public DateTimeOffset CreatedUtc  { get; set; }
    public DateTimeOffset UpdatedUtc  { get; set; }
}
