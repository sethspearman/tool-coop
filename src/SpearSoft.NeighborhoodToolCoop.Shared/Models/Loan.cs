namespace SpearSoft.NeighborhoodToolCoop.Shared.Models;

public class Loan
{
    public Guid   Id          { get; set; }
    public Guid   TenantId    { get; set; }
    public Guid   ToolId      { get; set; }
    public Guid   BorrowerId  { get; set; }
    public DateTimeOffset StartUtc    { get; set; }
    public DateTimeOffset DueUtc      { get; set; }
    public DateTimeOffset? ReturnedUtc { get; set; }
    public string Status     { get; set; } = "Reserved"; // loan_status enum
    public string? Notes     { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Populated by join queries
    public Tool? Tool     { get; set; }
    public User? Borrower { get; set; }
}
