using SpearSoft.NeighborhoodToolCoop.Shared.Contracts;
using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface IToolRepository
{
    Task<IEnumerable<Tool>>  ListAsync(ToolFilter filter);
    Task<Tool?>              GetByIdAsync(Guid id);
    Task<Tool?>              GetByQrCodeAsync(string qrCode);
    Task<Tool>               CreateAsync(Tool tool);
    Task                     UpdateAsync(Tool tool);
    Task                     SetActiveAsync(Guid id, bool isActive, Guid updatedBy);
}
