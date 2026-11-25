using Identity.Domain.Entities.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class IdentityScopeRepository : IIdentityScopeRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOrganizationScopeDirectory _directory;

        public IdentityScopeRepository(ApplicationDbContext dbContext, IOrganizationScopeDirectory directory)
        {
            _dbContext = dbContext;
            _directory = directory;
        }

        public async Task<IReadOnlyList<IdentityTenantEntry>> GetTenantsAsync(
            CancellationToken cancellationToken) =>
            (await _directory.GetTenantsAsync(cancellationToken))
                .Select(tenant => new IdentityTenantEntry(tenant.Id, tenant.Name, tenant.Status, tenant.CreatedAt))
                .ToArray();

        public async Task<IReadOnlyList<IdentityAdminScopeEntry>> GetTenantScopesAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants
                .AsNoTracking()
                .Where(item => item.Id == tenantId)
                .Select(item => new IdentityAdminScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Tenant,
                    null,
                    item.IsActive))
                .SingleOrDefaultAsync(cancellationToken);
            if (tenant is null)
                return [];

            var organizations = await _dbContext.Organizations
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId)
                .Select(item => new IdentityAdminScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Organization,
                    tenantId,
                    item.IsActive))
                .ToListAsync(cancellationToken);
            var branches = await _dbContext.Branches
                .AsNoTracking()
                .Where(item => item.Organization.TenantId == tenantId)
                .Select(item => new IdentityAdminScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Branch,
                    item.OrganizationId,
                    item.IsActive))
                .ToListAsync(cancellationToken);

            return new[] { tenant }.Concat(organizations).Concat(branches).ToArray();
        }

        public async Task<IdentityScopeEntry?> GetScopeAsync(
            Guid scopeId,
            CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants
                .AsNoTracking()
                .Where(item => item.Id == scopeId)
                .Select(item => new IdentityScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Tenant,
                    null))
                .SingleOrDefaultAsync(cancellationToken);
            if (tenant is not null)
                return tenant;

            var organization = await _dbContext.Organizations
                .AsNoTracking()
                .Where(item => item.Id == scopeId)
                .Select(item => new IdentityScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Organization,
                    item.TenantId))
                .SingleOrDefaultAsync(cancellationToken);
            if (organization is not null)
                return organization;

            return await _dbContext.Branches
                .AsNoTracking()
                .Where(item => item.Id == scopeId)
                .Select(item => new IdentityScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Branch,
                    item.OrganizationId))
                .SingleOrDefaultAsync(cancellationToken);
        }

    }
}
