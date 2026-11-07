using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class UserRoleAssignmentRepository : IUserRoleAssignmentRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRolePermissionRepository _scopeRepository;

        public UserRoleAssignmentRepository(
            ApplicationDbContext dbContext,
            IRolePermissionRepository scopeRepository)
        {
            _dbContext = dbContext;
            _scopeRepository = scopeRepository;
        }

        public async Task<IReadOnlyList<Guid>> GetRoleIdsAtScopeAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            CancellationToken cancellationToken)
        {
            var path = await _scopeRepository.GetScopePathAsync(tenantId, scopeId, cancellationToken);
            if (path is null) return [];
            var scopeType = path.BranchId.HasValue
                ? IdentityScopeType.Branch
                : path.OrganizationId.HasValue
                    ? IdentityScopeType.Organization
                    : IdentityScopeType.Tenant;
            return await (
                from assignment in _dbContext.UserRoleAssignments.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
                join role in _dbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                where assignment.UserId == userId
                    && user.TenantId == tenantId
                    && role.TenantId == tenantId
                    && assignment.ScopeType == scopeType
                    && assignment.ScopeId == scopeId
                select assignment.RoleId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetAllRoleIdsAsync(
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken) =>
            await (
                from assignment in _dbContext.UserRoleAssignments.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                where assignment.UserId == userId && role.TenantId == tenantId
                select assignment.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);

        public async Task<bool> ReplaceRoleIdsAtScopeAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            var path = await _scopeRepository.GetScopePathAsync(tenantId, scopeId, cancellationToken);
            if (path is null)
                return false;

            var userBelongsToTenant = await _dbContext.Users
                .AnyAsync(user => user.Id == userId && user.TenantId == tenantId, cancellationToken);
            if (!userBelongsToTenant)
                return false;

            var requestedRoleIds = roleIds.ToHashSet();
            if (await _dbContext.Roles.AnyAsync(
                    role => requestedRoleIds.Contains(role.Id)
                        && (role.Name == "Administrator" || role.TenantId != tenantId),
                    cancellationToken))
                return false;
            if (await _dbContext.Roles.CountAsync(
                    role => requestedRoleIds.Contains(role.Id) && role.TenantId == tenantId,
                    cancellationToken) != requestedRoleIds.Count)
                return false;

            var scopeType = path.BranchId.HasValue
                ? IdentityScopeType.Branch
                : path.OrganizationId.HasValue
                    ? IdentityScopeType.Organization
                    : IdentityScopeType.Tenant;
            var existing = await _dbContext.UserRoleAssignments
                .Where(assignment => assignment.UserId == userId
                    && assignment.ScopeType == scopeType
                    && assignment.ScopeId == scopeId)
                .ToListAsync(cancellationToken);
            var existingIds = existing.Select(assignment => assignment.RoleId).ToHashSet();
            _dbContext.UserRoleAssignments.RemoveRange(
                existing.Where(assignment => !requestedRoleIds.Contains(assignment.RoleId)));
            _dbContext.UserRoleAssignments.AddRange(
                requestedRoleIds
                    .Where(roleId => !existingIds.Contains(roleId))
                    .Select(roleId => new UserRoleAssignment
                    {
                        UserId = userId,
                        RoleId = roleId,
                        ScopeType = scopeType,
                        ScopeId = scopeId
                    }));
            return true;
        }
    }
}
