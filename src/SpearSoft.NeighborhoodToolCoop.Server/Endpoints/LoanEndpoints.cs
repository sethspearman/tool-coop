using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;
using System.Security.Claims;

namespace SpearSoft.NeighborhoodToolCoop.Server.Endpoints;

public static class LoanEndpoints
{
    public static void MapLoanEndpoints(this WebApplication app)
    {
        // GET /api/v1/loans/my?activeOnly=true
        app.MapGet("/api/v1/loans/my", async (
            bool?         activeOnly,
            ILoanRepository repo,
            HttpContext    ctx) =>
        {
            var borrowerId = RequireUserId(ctx);
            var loans = await repo.GetByBorrowerAsync(borrowerId, activeOnly ?? false);
            return Results.Ok(loans);
        }).RequireAuthorization();

        // POST /api/v1/loans  — creates a Reserved loan then immediately checks it out
        app.MapPost("/api/v1/loans", async (
            CreateLoanRequest req,
            ILoanRepository   loanRepo,
            IToolRepository   toolRepo,
            HttpContext        ctx) =>
        {
            var borrowerId = RequireUserId(ctx);

            // Prevent double-booking
            var existing = await loanRepo.GetActiveForToolAsync(req.ToolId);
            if (existing is not null)
                return Results.Conflict(new { error = "Tool is already checked out or reserved." });

            var loan = new Loan
            {
                ToolId      = req.ToolId,
                BorrowerId  = borrowerId,
                StartUtc    = DateTimeOffset.UtcNow,
                DueUtc      = req.DueUtc,
                Status      = "Reserved",
                Notes       = req.Notes,
                CreatedBy   = borrowerId
            };

            var created = await loanRepo.CreateAsync(loan);

            // Immediately transition to CheckedOut
            await loanRepo.CheckOutAsync(created.Id, borrowerId);
            created.Status = "CheckedOut";

            return Results.Created($"/api/v1/loans/{created.Id}", created);
        }).RequireAuthorization();

        // POST /api/v1/loans/{id}/return
        app.MapPost("/api/v1/loans/{id:guid}/return", async (
            Guid id,
            ReturnLoanRequest? req,
            ILoanRepository repo,
            HttpContext      ctx) =>
        {
            var userId = RequireUserId(ctx);
            await repo.ReturnAsync(id, userId, req?.Notes);
            return Results.NoContent();
        }).RequireAuthorization();

        // POST /api/v1/loans/{id}/cancel
        app.MapPost("/api/v1/loans/{id:guid}/cancel", async (
            Guid id,
            ILoanRepository repo,
            HttpContext      ctx) =>
        {
            var userId = RequireUserId(ctx);
            await repo.CancelAsync(id, userId);
            return Results.NoContent();
        }).RequireAuthorization();

        // GET /api/v1/loans/{id}
        app.MapGet("/api/v1/loans/{id:guid}", async (Guid id, ILoanRepository repo) =>
        {
            var loan = await repo.GetByIdAsync(id);
            return loan is null ? Results.NotFound() : Results.Ok(loan);
        }).RequireAuthorization();
    }

    private static Guid RequireUserId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirstValue("app_user_id"), out var id)
            ? id
            : throw new UnauthorizedAccessException("User ID claim missing.");
}

public class ReturnLoanRequest
{
    public string? Notes { get; set; }
}
