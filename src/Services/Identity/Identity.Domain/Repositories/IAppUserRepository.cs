using Identity.Domain.Entities.Identity;

namespace Identity.Domain.Repositories
{
    public interface IAppUserRepository
    {
        Task<int> CountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default);
        Task<List<AppUser>> GetPageAsync(
            Guid tenantId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
    }
}