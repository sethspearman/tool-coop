namespace SpearSoft.NeighborhoodToolCoop.Shared.Contracts;

public class CreateToolRequest
{
    public string  Name            { get; set; } = string.Empty;
    public string? Description     { get; set; }
    public string? Category        { get; set; }
    public string  OwnerType       { get; set; } = "Coop";
    public Guid?   OwnerUserId     { get; set; }
    public string  Condition       { get; set; } = "Good";
    public Guid?   LocationId      { get; set; }
    public string  QrCode          { get; set; } = string.Empty;
    public string? ImageUrl        { get; set; }
    public decimal? ValueEstimate  { get; set; }
    public decimal DepositRequired { get; set; }
}
