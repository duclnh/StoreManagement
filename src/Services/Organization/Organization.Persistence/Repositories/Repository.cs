using Microsoft.EntityFrameworkCore;
using Organization.Domain.Abstractions;
using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Repositories;
using TenantEntity = global::Organization.Domain.Entities.Tenants.Tenant;
using OrganizationEntity = global::Organization.Domain.Entities.Organizations.Organization;
using BranchEntity = global::Organization.Domain.Entities.Branches.Branch;
using WarehouseEntity = global::Organization.Domain.Entities.Warehouses.Warehouse;
using PointOfSaleEntity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Persistence.Repositories
{
    public sealed class Repository<T> : IRepository<T> where T : ScopedEntity
    {
        private readonly ApplicationDbContext _db;
        private readonly ITenantContext _context;
        private readonly IOrganizationContext _organizationContext;

        public Repository(ApplicationDbContext db, ITenantContext context, IOrganizationContext organizationContext)
        {
            _db = db;
            _context = context;
            _organizationContext = organizationContext;
        }

        private IQueryable<T> Scoped()
        {
            var query = _db.Set<T>().AsQueryable();
            if (_context.IsSystemAdministrator)
            {
                return query;
            }

            if (_context.TenantId is not Guid tenantId || _context.UserId is not Guid userId)
            {
                return query.Where(x => false);
            }

            query = query.Where(x => x.TenantId == tenantId);
            if (_context.IsTenantAdministrator)
            {
                return query;
            }

            var memberships = _db.Members.Where(x => x.TenantId == tenantId && x.UserId == userId && x.Status
                && _db.Organizations.Any(o => o.Id == x.OrganizationId && o.TenantId == tenantId && o.Status));
            if (_organizationContext.OrganizationId is Guid organizationId)
            {
                memberships = memberships.Where(x => x.OrganizationId == organizationId);
            }

            if (_organizationContext.BranchId is Guid branchId)
            {
                memberships = memberships.Where(x => x.BranchId == null || x.BranchId == branchId);
            }

            if (typeof(T) == typeof(TenantEntity))
            {
                return query.Where(x => false);
            }

            if (typeof(T) == typeof(OrganizationEntity))
            {
                return query.Where(x => memberships.Any(m => m.OrganizationId == x.Id));
            }

            if (typeof(T) == typeof(BranchEntity))
            {
                var branches = _db.Branches.Where(b => memberships.Any(m => m.OrganizationId == b.OrganizationId && (m.BranchId == null || m.BranchId == b.Id)));
                if (_organizationContext.BranchId is Guid requestedBranch)
                {
                    branches = branches.Where(b => b.Id == requestedBranch);
                }

                return query.Where(x => branches.Any(b => b.Id == x.Id));
            }

            var accessibleBranches = _db.Branches.Where(b => b.Status && memberships.Any(m => m.OrganizationId == b.OrganizationId && (m.BranchId == null || m.BranchId == b.Id)));
            if (_organizationContext.BranchId is Guid requestedChildBranch)
            {
                accessibleBranches = accessibleBranches.Where(b => b.Id == requestedChildBranch);
            }

            return query.Where(x => accessibleBranches.Any(b => b.Id == EF.Property<Guid>(x, "BranchId")));
        }

        public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Scoped().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        private IQueryable<T> Filter(Guid? parentId, string? search)
        {
            var query = Scoped();
            if (parentId is Guid parent)
            {
                var property = typeof(T) == typeof(OrganizationEntity) ? "TenantId"
                    : typeof(T) == typeof(BranchEntity) ? "OrganizationId" : "BranchId";
                query = query.Where(x => EF.Property<Guid>(x, property) == parent);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
            }

            return query;
        }

        public async Task<IReadOnlyList<T>> ListAsync(Guid? parentId, string? search, int page, int pageSize, CancellationToken cancellationToken) =>
            await Filter(parentId, search).AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        public Task<int> CountAsync(Guid? parentId, string? search, CancellationToken cancellationToken) =>
            Filter(parentId, search).CountAsync(cancellationToken);

        public Task<bool> CodeExistsAsync(Guid tenantId, Guid? parentId, string code, Guid? excludingId, CancellationToken cancellationToken)
        {
            var query = _db.Set<T>().Where(x => x.Code == code && x.Id != excludingId);
            if (typeof(T) != typeof(TenantEntity))
            {
                query = query.Where(x => x.TenantId == tenantId);
            }

            if (parentId is Guid parent)
            {
                var property = typeof(T) == typeof(BranchEntity) ? "OrganizationId" : "BranchId";
                query = query.Where(x => EF.Property<Guid>(x, property) == parent);
            }

            return query.AnyAsync(cancellationToken);
        }

        public async Task ClearDefaultAsync(Guid tenantId, Guid parentId, Guid excludingId, CancellationToken cancellationToken)
        {
            var property = typeof(T) == typeof(BranchEntity) ? "OrganizationId" : "BranchId";
            await _db.Set<T>().Where(x => x.TenantId == tenantId && x.Id != excludingId
                && EF.Property<Guid>(x, property) == parentId && EF.Property<bool>(x, "IsDefault"))
                .ExecuteUpdateAsync(update => update.SetProperty(x => EF.Property<bool>(x, "IsDefault"), false), cancellationToken);
        }

        public void Add(T entity) => _db.Set<T>().Add(entity);
    }
}
