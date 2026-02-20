using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Net.Http.Json;

namespace SpearSoft.NeighborhoodToolCoop.Client.Services;

public class LoanApiService(HttpClient http)
{
    public async Task<List<Loan>> GetMyLoansAsync(bool activeOnly = false)
    {
        var url = activeOnly ? "/api/v1/loans/my?activeOnly=true" : "/api/v1/loans/my";
        return await http.GetFromJsonAsync<List<Loan>>(url) ?? [];
    }

    public async Task<Loan?> GetByIdAsync(Guid id) =>
        await http.GetFromJsonAsync<Loan>($"/api/v1/loans/{id}");

    /// <summary>Creates a loan and immediately checks it out. Returns the created loan.</summary>
    public async Task<(bool Success, string? Error, Loan? Loan)> CheckOutAsync(
        Guid toolId, DateTimeOffset dueUtc, string? notes = null)
    {
        var req  = new CreateLoanRequest { ToolId = toolId, DueUtc = dueUtc, Notes = notes };
        var resp = await http.PostAsJsonAsync("/api/v1/loans", req);

        if (resp.IsSuccessStatusCode)
            return (true, null, await resp.Content.ReadFromJsonAsync<Loan>());

        var problem = await resp.Content.ReadAsStringAsync();
        return (false, problem, null);
    }

    public async Task<bool> ReturnAsync(Guid loanId, string? notes = null)
    {
        var resp = await http.PostAsJsonAsync($"/api/v1/loans/{loanId}/return",
            new { Notes = notes });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CancelAsync(Guid loanId)
    {
        var resp = await http.PostAsJsonAsync($"/api/v1/loans/{loanId}/cancel", new { });
        return resp.IsSuccessStatusCode;
    }
}
