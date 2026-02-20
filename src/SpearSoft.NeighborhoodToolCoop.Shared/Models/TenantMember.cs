namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

/// <summary>Maps to tenant_users. Bridges a user to their role within a tenant.</summary>
public class TenantMember
{
    public Guid   TenantId   { get; set; }
    public Guid   UserId     { get; set; }
    public string Role       { get; set; } = "Member";  // user_role enum
    public string Status     { get; set; } = "Pending"; // member_status enum
    public DateTimeOffset JoinedUtc  { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }

    // Populated by join queries
    public User? User { get; set; }
}
