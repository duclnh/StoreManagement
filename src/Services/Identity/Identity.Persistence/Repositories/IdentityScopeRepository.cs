using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class IdentityScopeRepository : IIdentityScopeRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public IdentityScopeRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<IdentityTenantEntry>> GetTenantsAsync(
            CancellationToken cancellationToken) =>
            await _dbContext.Tenants
                .AsNoTracking()
                .OrderBy(tenant => tenant.Name)
                .Select(tenant => new IdentityTenantEntry(
                    tenant.Id,
                    tenant.Name,
                    tenant.IsActive,
                    tenant.CreatedAtUtc))
                .ToListAsync(cancellationToken);

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

        public async Task<IdentityScopeMutationResult> CreateTenantAsync(
            string name,
            CancellationToken cancellationToken)
        {
            if (await _dbContext.Tenants.AnyAsync(tenant => tenant.Name == name, cancellationToken))
                return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Conflict);

            var tenant = new Tenant { Id = Guid.NewGuid(), Name = name };
            _dbContext.Tenants.Add(tenant);
            return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Succeeded, tenant.Id);
        }

        public async Task<IdentityScopeMutationResult> CreateOrganizationAsync(
            Guid tenantId,
            string name,
            CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants
                .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
            if (tenant is null || !tenant.IsActive)
                return new IdentityScopeMutationResult(IdentityScopeMutationStatus.NotFound);

            if (await _dbContext.Organizations.AnyAsync(
                    organization => organization.TenantId == tenantId && organization.Name == name,
                    cancellationToken))
                return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Conflict);

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name
            };
            _dbContext.Organizations.Add(organization);
            return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Succeeded, organization.Id);
        }

        public async Task<IdentityScopeMutationResult> CreateBranchAsync(
            Guid tenantId,
            Guid organizationId,
            string name,
            CancellationToken cancellationToken)
        {
            var organization = await _dbContext.Organizations
                .SingleOrDefaultAsync(item => item.Id == organizationId
                    && item.TenantId == tenantId
                    && item.IsActive
                    && item.Tenant.IsActive,
                    cancellationToken);
            if (organization is null)
                return new IdentityScopeMutationResult(IdentityScopeMutationStatus.NotFound);

            if (await _dbContext.Branches.AnyAsync(
                    branch => branch.OrganizationId == organizationId && branch.Name == name,
                    cancellationToken))
                return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Conflict);

            var branch = new Branch
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = name
            };
            _dbContext.Branches.Add(branch);
            return new IdentityScopeMutationResult(IdentityScopeMutationStatus.Succeeded, branch.Id);
        }

        public async Task<IdentityScopeMutationStatus> SetTenantActiveAsync(
            Guid tenantId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants
                .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
            if (tenant is null)
                return IdentityScopeMutationStatus.NotFound;

            tenant.IsActive = isActive;
            return IdentityScopeMutationStatus.Succeeded;
        }

        public async Task<IdentityScopeMutationStatus> SetOrganizationActiveAsync(
            Guid tenantId,
            Guid organizationId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var organization = await _dbContext.Organizations
                .SingleOrDefaultAsync(item => item.Id == organizationId && item.TenantId == tenantId,
                    cancellationToken);
            if (organization is null || (isActive && !organization.Tenant.IsActive))
                return IdentityScopeMutationStatus.NotFound;

            organization.IsActive = isActive;
            return IdentityScopeMutationStatus.Succeeded;
        }

        public async Task<IdentityScopeMutationStatus> SetBranchActiveAsync(
            Guid tenantId,
            Guid organizationId,
            Guid branchId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var branch = await _dbContext.Branches
                .SingleOrDefaultAsync(item => item.Id == branchId
                    && item.OrganizationId == organizationId
                    && item.Organization.TenantId == tenantId,
                    cancellationToken);
            if (branch is null
                || (isActive && (!branch.Organization.IsActive || !branch.Organization.Tenant.IsActive)))
                return IdentityScopeMutationStatus.NotFound;

            branch.IsActive = isActive;
            return IdentityScopeMutationStatus.Succeeded;
        }
    }
}
