using Microsoft.EntityFrameworkCore;
using Organization.Domain.Repositories;

using Organization.Persistence;

namespace Organization.Persistence.Repositories
{
    // A read adapter for authenticated service-to-service directory calls only.
    public sealed class ScopeDirectory : IScopeDirectoryRepository
    {
        private readonly ApplicationDbContext _db;

        public ScopeDirectory(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<TenantScope>> GetTenantsAsync(CancellationToken cancellationToken) =>
            await _db.Tenants.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Select(x => new TenantScope(x.Id, x.Code, x.Name, x.Status, x.CreatedAt)).ToListAsync(cancellationToken);

        public async Task<ScopeSnapshot?> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var tenant = await _db.Tenants.AsNoTracking().Where(x => x.Id == tenantId)
                .Select(x => new TenantScope(x.Id, x.Code, x.Name, x.Status, x.CreatedAt))
                .SingleOrDefaultAsync(cancellationToken);
            if (tenant is null)
            {
                return null;
            }

            var organizations = await _db.Organizations.AsNoTracking().Where(x => x.TenantId == tenantId)
                .Select(x => new OrganizationScope(x.Id, x.TenantId, x.Name, x.Status)).ToListAsync(cancellationToken);
            var branches = await _db.Branches.AsNoTracking().Where(x => x.TenantId == tenantId)
                .Select(x => new BranchScope(x.Id, x.OrganizationId, x.Name, x.Status)).ToListAsync(cancellationToken);
            return new ScopeSnapshot(tenant, organizations, branches);
        }
    }
}
