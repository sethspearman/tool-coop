using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface ILoanRepository
{
    Task<Loan?>             GetByIdAsync(Guid id);
    Task<IEnumerable<Loan>> GetByBorrowerAsync(Guid borrowerId, bool activeOnly = false);

    /// <summary>Returns the currently active (CheckedOut/Reserved) loan for a tool, if any.</summary>
    Task<Loan?>             GetActiveForToolAsync(Guid toolId);

    /// <summary>All CheckedOut loans past their due_utc — used by the Hangfire overdue job.</summary>
    Task<IEnumerable<Loan>> GetOverdueAsync();

    Task<Loan>              CreateAsync(Loan loan);
    Task                    CheckOutAsync(Guid id, Guid updatedBy);
    Task                    ReturnAsync(Guid id, Guid updatedBy, string? notes = null);
    Task                    CancelAsync(Guid id, Guid updatedBy);
    Task                    MarkOverdueAsync(IEnumerable<Guid> loanIds);
}
