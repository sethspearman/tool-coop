namespace SpearSoft.NeighborhoodToolCoop.Shared.Contracts;

public class CurrentUserDto
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string TenantSlug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
