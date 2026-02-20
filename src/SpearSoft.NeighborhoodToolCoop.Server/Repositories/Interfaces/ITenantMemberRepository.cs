using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface ITenantMemberRepository
{
    Task<TenantMember?>            GetAsync(Guid userId);
    Task<IEnumerable<TenantMember>> ListAsync();
    Task                           UpsertAsync(TenantMember member);
    Task                           UpdateRoleAsync(Guid userId, string role);
    Task                           UpdateStatusAsync(Guid userId, string status);
}
