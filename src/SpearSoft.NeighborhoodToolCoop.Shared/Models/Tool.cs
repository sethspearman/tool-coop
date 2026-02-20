namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class Tool
{
    public Guid    Id              { get; set; }
    public Guid    TenantId        { get; set; }
    public string  Name            { get; set; } = string.Empty;
    public string? Description     { get; set; }
    public string? Category        { get; set; }
    public string  OwnerType       { get; set; } = "Coop";  // owner_type enum
    public Guid?   OwnerUserId     { get; set; }
    public string  Condition       { get; set; } = "Good";  // tool_condition enum
    public Guid?   LocationId      { get; set; }
    public string  QrCode          { get; set; } = string.Empty;
    public string? ImageUrl        { get; set; }
    public decimal? ValueEstimate  { get; set; }
    public decimal DepositRequired { get; set; }
    public bool    IsActive        { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Populated by join queries
    public Location? Location   { get; set; }
    public List<ToolAttribute> Attributes { get; set; } = [];
}
