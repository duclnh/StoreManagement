using Identity.Domain.Entities.Identity;

namespace Identity.Domain.Repositories
{
    public interface IAppUserRepository
    {
        Task<int> CountAsync(string? search, CancellationToken cancellationToken = default);
        Task<List<AppUser>> GetPageAsync(string? search, int skip, int take, CancellationToken cancellationToken = default);
    }
}