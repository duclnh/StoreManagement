using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class AppUserRepository : IAppUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AppUserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> CountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default) =>
            Filter(tenantId, search).CountAsync(cancellationToken);

        public Task<List<AppUser>> GetPageAsync(
            Guid tenantId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Filter(tenantId, search)
                .OrderBy(user => user.Email)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

        private IQueryable<AppUser> Filter(Guid tenantId, string? search)
        {
            var query = _dbContext.AppUsers.AsNoTracking().Where(user => user.TenantId == tenantId);
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var term = search.Trim();
            return query.Where(user =>
                (user.Email != null && user.Email.Contains(term))
                || user.FirstName.Contains(term)
                || user.LastName.Contains(term)
                || user.FullName.Contains(term));
        }
    }
}