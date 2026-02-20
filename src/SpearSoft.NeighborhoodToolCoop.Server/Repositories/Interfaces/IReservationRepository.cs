using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?>              GetByIdAsync(Guid id);
    Task<IEnumerable<Reservation>>  GetByToolAsync(Guid toolId);
    Task<IEnumerable<Reservation>>  GetByUserAsync(Guid userId);
    Task<Reservation>               CreateAsync(Reservation reservation);
    Task                            ApproveAsync(Guid id);
    Task                            CancelAsync(Guid id);
}
