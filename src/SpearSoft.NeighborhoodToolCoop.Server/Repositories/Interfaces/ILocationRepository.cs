using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<Location?>              GetByIdAsync(Guid id);
    Task<IEnumerable<Location>>  ListAllAsync();
    Task<IEnumerable<Location>>  GetChildrenAsync(Guid parentId);

    /// <summary>Returns all locations whose ltree path is a descendant of rootPath.</summary>
    Task<IEnumerable<Location>>  GetSubtreeAsync(string rootPath);

    Task<Location>               CreateAsync(Location location);
    Task                         UpdateAsync(Location location);

    /// <summary>
    /// Moves a location (and its entire subtree) to a new parent.
    /// Updates the ltree path prefix for all affected rows.
    /// </summary>
    Task                         MoveAsync(Guid id, Guid? newParentId, string newPath);

    Task                         DeleteAsync(Guid id);
}
