namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class User
{
    public Guid   Id            { get; set; }
    public Guid   TenantId      { get; set; }
    public string DisplayName   { get; set; } = string.Empty;
    public string Email         { get; set; } = string.Empty;
    public string? Phone        { get; set; }
    public string? AvatarUrl    { get; set; }
    public string GoogleSubject { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
