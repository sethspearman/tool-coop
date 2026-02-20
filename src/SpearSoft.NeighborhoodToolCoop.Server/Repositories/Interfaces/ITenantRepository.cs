using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

/// <summary>Not tenant-scoped — used for tenant resolution and admin operations.</summary>
public interface ITenantRepository
{
    Task<Tenant?> GetBySlugAsync(string slug);
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant>  CreateAsync(Tenant tenant);
    Task          UpdateAsync(Tenant tenant);
}
