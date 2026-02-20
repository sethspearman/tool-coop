using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<Notification>              CreateAsync(Notification notification);
    Task<IEnumerable<Notification>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Notification>> GetPendingAsync();
    Task                            MarkSentAsync(Guid id);
    Task                            MarkFailedAsync(Guid id);
}
