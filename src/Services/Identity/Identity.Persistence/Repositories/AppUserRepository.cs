using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Identity.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class AppUserRepository : IAppUserRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IIdentityTenantContext _tenantContext;

        public AppUserRepository(ApplicationDbContext dbContext, IIdentityTenantContext tenantContext)
        {
            _dbContext = dbContext;
            _tenantContext = tenantContext;
        }

        public Task<bool> IsInScopeAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
            Filter(tenantId, null).AnyAsync(user => user.Id == userId, cancellationToken);

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
            var scopeId = _tenantContext.RequestedScopeId;
            if (scopeId is null) return query.Where(user => false);
            if (scopeId != tenantId)
            {
                // Membership is explicit: an organization includes its branches, never tenant-wide users.
                var branches = _dbContext.Branches.Where(branch => branch.OrganizationId == scopeId
                    && branch.Organization.TenantId == tenantId && branch.IsActive && branch.Organization.IsActive);
                var members = _dbContext.UserRoleAssignments.Where(assignment =>
                    assignment.ScopeId == scopeId || branches.Any(branch => branch.Id == assignment.ScopeId));
                query = query.Where(user => members.Any(assignment => assignment.UserId == user.Id));
            }
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
