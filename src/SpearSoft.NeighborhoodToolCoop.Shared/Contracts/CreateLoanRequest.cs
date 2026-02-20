namespace SpearSoft.NeighborhoodToolCoop.Shared.Contracts;

public class CreateLoanRequest
{
    public Guid            ToolId  { get; set; }
    public DateTimeOffset  DueUtc  { get; set; }
    public string?         Notes   { get; set; }
}
