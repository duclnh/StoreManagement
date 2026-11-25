using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    // Identity owns these local read models, never the business source of tenant/organization/branch data.
    public sealed class OrganizationScopeProjection : IOrganizationScopeProjection
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrganizationScopeDirectory _directory;

        public OrganizationScopeProjection(ApplicationDbContext db, IOrganizationScopeDirectory directory)
        {
            _db = db;
            _directory = directory;
        }

        public async Task<bool> RefreshAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var snapshot = await _directory.GetAsync(tenantId, cancellationToken);
            if (snapshot is null)
            {
                return false;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            // Serialize competing refreshes of a tenant without locking unrelated tenants.
            var lockResource = $"OrganizationScopeProjection:{tenantId:D}";
            await _db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sp_getapplock @Resource={lockResource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @result < 0 THROW 51000, 'Scope projection lock unavailable.', 1;", cancellationToken);

            var tenant = await _db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
            if (tenant is null)
            {
                tenant = new Tenant { Id = tenantId };
                _db.Tenants.Add(tenant);
            }

            tenant.Code = snapshot.Tenant.Code;
            tenant.Name = snapshot.Tenant.Name;
            tenant.IsActive = snapshot.Tenant.Status;
            tenant.CreatedAtUtc = snapshot.Tenant.CreatedAt;

            var organizations = await _db.Organizations.Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
            foreach (var item in organizations)
            {
                item.IsActive = false;
            }

            foreach (var item in snapshot.Organizations)
            {
                var entity = organizations.SingleOrDefault(x => x.Id == item.Id);
                if (entity is null)
                {
                    entity = new Domain.Entities.Identity.Organization { Id = item.Id, TenantId = tenantId };
                    _db.Organizations.Add(entity);
                }

                entity.Name = item.Name;
                entity.IsActive = item.Status;
            }

            var branches = await _db.Branches.Where(x => x.Organization.TenantId == tenantId).ToListAsync(cancellationToken);
            foreach (var item in branches)
            {
                item.IsActive = false;
            }

            foreach (var item in snapshot.Branches)
            {
                var entity = branches.SingleOrDefault(x => x.Id == item.Id);
                if (entity is null)
                {
                    entity = new Branch { Id = item.Id, OrganizationId = item.OrganizationId };
                    _db.Branches.Add(entity);
                }

                entity.Name = item.Name;
                entity.IsActive = item.Status;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return snapshot.Tenant.Status;
        }
    }
}
