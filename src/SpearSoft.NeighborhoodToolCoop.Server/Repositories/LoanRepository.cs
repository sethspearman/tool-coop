using Dapper;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories;

public class LoanRepository(DbConnectionFactory dbFactory, TenantContext tenant)
    : RepositoryBase(dbFactory, tenant), ILoanRepository
{
    private const string SelectWithJoins = """
        SELECT l.id, l.tenant_id, l.tool_id, l.borrower_id, l.start_utc, l.due_utc,
               l.returned_utc, l.status, l.notes, l.created_utc, l.updated_utc,
               t.id, t.tenant_id, t.name, t.qr_code, t.condition, t.is_active,
               t.owner_type, t.location_id, t.created_utc, t.updated_utc,
               u.id, u.tenant_id, u.display_name, u.email, u.avatar_url,
               u.google_subject, u.created_utc, u.updated_utc
          FROM loans l
          JOIN tools t ON t.id = l.tool_id
          JOIN users u ON u.id = l.borrower_id
        """;

    private static Task<IEnumerable<Loan>> MapLoans(Dapper.SqlMapper.GridReader? _ = null) =>
        Task.FromResult(Enumerable.Empty<Loan>()); // placeholder; real mapping below

    private static Loan Map(Loan loan, Tool tool, User borrower)
    {
        loan.Tool = tool;
        loan.Borrower = borrower;
        return loan;
    }

    public async Task<Loan?> GetByIdAsync(Guid id)
    {
        using var conn = OpenConnection();
        var results = await conn.QueryAsync<Loan, Tool, User, Loan>(
            SelectWithJoins + " WHERE l.tenant_id = @TenantId AND l.id = @Id",
            Map, new { TenantId, Id = id }, splitOn: "id,id");
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Loan>> GetByBorrowerAsync(Guid borrowerId, bool activeOnly = false)
    {
        using var conn = OpenConnection();
        var sql = SelectWithJoins +
            " WHERE l.tenant_id = @TenantId AND l.borrower_id = @BorrowerId";
        if (activeOnly)
            sql += " AND l.status IN ('Reserved', 'CheckedOut', 'Overdue')";
        sql += " ORDER BY l.due_utc";

        return await conn.QueryAsync<Loan, Tool, User, Loan>(
            sql, Map, new { TenantId, BorrowerId = borrowerId }, splitOn: "id,id");
    }

    public async Task<Loan?> GetActiveForToolAsync(Guid toolId)
    {
        using var conn = OpenConnection();
        var results = await conn.QueryAsync<Loan, Tool, User, Loan>(
            SelectWithJoins +
            " WHERE l.tenant_id = @TenantId AND l.tool_id = @ToolId AND l.status IN ('Reserved', 'CheckedOut')",
            Map, new { TenantId, ToolId = toolId }, splitOn: "id,id");
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Loan>> GetOverdueAsync()
    {
        using var conn = OpenConnection();
        return await conn.QueryAsync<Loan, Tool, User, Loan>(
            SelectWithJoins +
            " WHERE l.tenant_id = @TenantId AND l.status = 'CheckedOut' AND l.due_utc < NOW()",
            Map, new { TenantId }, splitOn: "id,id");
    }

    public async Task<Loan> CreateAsync(Loan loan)
    {
        loan.TenantId = TenantId;
        using var conn = OpenConnection();
        loan.Id = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO loans (tenant_id, tool_id, borrower_id, start_utc, due_utc, status, notes, created_by)
            VALUES (@TenantId, @ToolId, @BorrowerId, @StartUtc, @DueUtc, @Status::loan_status, @Notes, @CreatedBy)
            RETURNING id
            """, loan);
        return loan;
    }

    public async Task CheckOutAsync(Guid id, Guid updatedBy)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE loans
               SET status     = 'CheckedOut'::loan_status,
                   start_utc  = NOW(),
                   updated_by = @UpdatedBy
             WHERE tenant_id = @TenantId AND id = @Id AND status = 'Reserved'
            """, new { TenantId, Id = id, UpdatedBy = updatedBy });
    }

    public async Task ReturnAsync(Guid id, Guid updatedBy, string? notes = null)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE loans
               SET status       = 'Returned'::loan_status,
                   returned_utc = NOW(),
                   notes        = COALESCE(@Notes, notes),
                   updated_by   = @UpdatedBy
             WHERE tenant_id = @TenantId AND id = @Id
               AND status IN ('CheckedOut', 'Overdue')
            """, new { TenantId, Id = id, Notes = notes, UpdatedBy = updatedBy });
    }

    public async Task CancelAsync(Guid id, Guid updatedBy)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE loans
               SET status     = 'Canceled'::loan_status,
                   updated_by = @UpdatedBy
             WHERE tenant_id = @TenantId AND id = @Id AND status = 'Reserved'
            """, new { TenantId, Id = id, UpdatedBy = updatedBy });
    }

    public async Task MarkOverdueAsync(IEnumerable<Guid> loanIds)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            UPDATE loans
               SET status = 'Overdue'::loan_status
             WHERE tenant_id = @TenantId
               AND id        = ANY(@Ids)
               AND status    = 'CheckedOut'
            """, new { TenantId, Ids = loanIds.ToArray() });
    }
}
