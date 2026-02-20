using SpearSoft.NeighborhoodToolCoop.Server.Services;
using System.Data;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Base;

/// <summary>
/// Base for all tenant-scoped repositories.
/// Enforces that TenantContext is resolved before any query runs,
/// satisfying the spec requirement: "tenant_id enforced in all Dapper queries."
/// </summary>
public abstract class RepositoryBase(DbConnectionFactory dbFactory, TenantContext tenant)
{
    protected Guid TenantId => tenant.IsResolved
        ? tenant.TenantId
        : throw new InvalidOperationException(
            "TenantContext is not resolved. All tenanted queries require a valid /t/{slug} route.");

    protected IDbConnection OpenConnection() => dbFactory.Create();
}
