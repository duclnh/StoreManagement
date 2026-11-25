using Microsoft.EntityFrameworkCore;
using Organization.Domain.Entities.Members;
using Organization.Domain.Repositories;

namespace Organization.Persistence.Repositories
{
    public sealed class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _db;

        public MemberRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        private IQueryable<OrganizationMember> Query(Guid tenantId, Guid organizationId, Guid? branchId) =>
            _db.Members.Where(x => x.TenantId == tenantId && x.OrganizationId == organizationId && x.BranchId == branchId);

        public async Task<IReadOnlyList<OrganizationMember>> ListAsync(Guid tenantId, Guid organizationId, Guid? branchId, int page, int pageSize, CancellationToken cancellationToken) =>
            await ListQuery(tenantId, organizationId, branchId).AsNoTracking().OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        public Task<int> CountAsync(Guid tenantId, Guid organizationId, Guid? branchId, CancellationToken cancellationToken) =>
            ListQuery(tenantId, organizationId, branchId).CountAsync(cancellationToken);

        private IQueryable<OrganizationMember> ListQuery(Guid tenantId, Guid organizationId, Guid? branchId) =>
            _db.Members.Where(x => x.TenantId == tenantId && x.OrganizationId == organizationId
                && (branchId == null || x.BranchId == branchId));

        public async Task<int> RemoveOrganizationAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken cancellationToken)
        {
            var members = await _db.Members.Where(x => x.TenantId == tenantId && x.OrganizationId == organizationId
                && x.UserId == userId).ToListAsync(cancellationToken);
            _db.Members.RemoveRange(members);
            return members.Count;
        }

        public Task<OrganizationMember?> FindAsync(Guid tenantId, Guid organizationId, Guid userId, Guid? branchId, CancellationToken cancellationToken) =>
            Query(tenantId, organizationId, branchId).SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        public Task<bool> HasAccessAsync(Guid tenantId, Guid organizationId, Guid? branchId, Guid userId, CancellationToken cancellationToken) =>
            _db.Members.AnyAsync(x => x.TenantId == tenantId && x.OrganizationId == organizationId && x.UserId == userId
                && x.Status && (x.BranchId == null || branchId != null && x.BranchId == branchId), cancellationToken);

        public async Task ClearDefaultAsync(Guid tenantId, Guid userId, Guid excludingId, CancellationToken cancellationToken)
        {
            await _db.Members.Where(x => x.TenantId == tenantId && x.UserId == userId && x.Id != excludingId && x.IsDefault)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        public void Add(OrganizationMember member) => _db.Members.Add(member);
        public void Remove(OrganizationMember member) => _db.Members.Remove(member);
    }
}
