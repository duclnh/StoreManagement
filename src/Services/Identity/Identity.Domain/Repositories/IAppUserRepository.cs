using Identity.Domain.Entities.Identity;

namespace Identity.Domain.Repositories
{
    public interface IAppUserRepository
    {
        Task<bool> IsInScopeAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default);
        Task<List<AppUser>> GetPageAsync(
            Guid tenantId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
    }
}
