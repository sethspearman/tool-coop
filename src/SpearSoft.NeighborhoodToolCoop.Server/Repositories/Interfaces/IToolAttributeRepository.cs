using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface IToolAttributeRepository
{
    Task<IEnumerable<ToolAttribute>> GetByToolAsync(Guid toolId);
    Task                             UpsertAsync(ToolAttribute attribute);
    Task                             DeleteAsync(Guid toolId, string key);
    Task                             DeleteAllForToolAsync(Guid toolId);
}
