using SpearSoft.NeighborhoodToolCoop.Shared.Models;

namespace SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?>  GetByIdAsync(Guid id);
    Task<User?>  GetByGoogleSubjectAsync(string googleSubject);
    Task<User>   CreateAsync(User user);
    Task         UpdateAsync(User user);
}
