using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public RolePermissionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> RoleExistsAsync(Guid roleId, Guid tenantId, CancellationToken cancellationToken) =>
            _dbContext.Roles.AnyAsync(
                role => role.Id == roleId && role.TenantId == tenantId,
                cancellationToken);

        public async Task<IdentityScopePath?> GetScopePathAsync(
            Guid tenantId,
            Guid scopeId,
            CancellationToken cancellationToken)
        {
            if (!await _dbContext.Tenants.AnyAsync(
                    tenant => tenant.Id == tenantId && tenant.IsActive,
                    cancellationToken))
                return null;

            if (scopeId == tenantId)
                return new IdentityScopePath(tenantId, null, null);

            var organization = await _dbContext.Organizations
                .AsNoTracking()
                .Where(item => item.Id == scopeId && item.TenantId == tenantId && item.IsActive)
                .Select(item => new { item.Id })
                .SingleOrDefaultAsync(cancellationToken);
            if (organization is not null)
                return new IdentityScopePath(tenantId, organization.Id, null);

            var branch = await _dbContext.Branches
                .AsNoTracking()
                .Where(item => item.Id == scopeId
                    && item.Organization.TenantId == tenantId
                    && item.IsActive
                    && item.Organization.IsActive)
                .Select(item => new { item.Id, OrganizationId = item.Organization.Id })
                .SingleOrDefaultAsync(cancellationToken);

            return branch is null
                ? null
                : new IdentityScopePath(tenantId, branch.OrganizationId, branch.Id);
        }

        public async Task<IReadOnlyList<IdentityScopeEntry>> GetScopesAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants
                .AsNoTracking()
                .Where(item => item.Id == tenantId && item.IsActive)
                .Select(item => new IdentityScopeEntry(item.Id, item.Name, (int)IdentityScopeType.Tenant, null))
                .SingleOrDefaultAsync(cancellationToken);
            if (tenant is null)
                return [];

            var organizations = await _dbContext.Organizations
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.IsActive)
                .Select(item => new IdentityScopeEntry(item.Id, item.Name, (int)IdentityScopeType.Organization, tenantId))
                .ToListAsync(cancellationToken);
            var branches = await _dbContext.Branches
                .AsNoTracking()
                .Where(item => item.Organization.TenantId == tenantId
                    && item.Organization.IsActive
                    && item.IsActive)
                .Select(item => new IdentityScopeEntry(
                    item.Id,
                    item.Name,
                    (int)IdentityScopeType.Branch,
                    item.OrganizationId))
                .ToListAsync(cancellationToken);

            return new[] { tenant }.Concat(organizations).Concat(branches).ToArray();
        }

        public async Task<IReadOnlyList<RolePermissionEntry>> GetRolePermissionsAsync(
            Guid roleId,
            Guid tenantId,
            Guid scopeId,
            CancellationToken cancellationToken)
        {
            if (await GetScopePathAsync(tenantId, scopeId, cancellationToken) is null
                || !await RoleExistsAsync(roleId, tenantId, cancellationToken))
                return [];

            var grantedPermissions = await _dbContext.Permissions
                .Where(permission => permission.RoleId == roleId && permission.ScopeId == scopeId)
                .Select(permission => new { permission.FunctionId, permission.ActionId })
                .ToListAsync(cancellationToken);
            var grantedSet = grantedPermissions
                .Select(permission => (permission.FunctionId, permission.ActionId))
                .ToHashSet();

            var catalog = await (
                from function in _dbContext.Functions.AsNoTracking()
                where function.IsActive != false
                join actionInFunction in _dbContext.ActionInFunctions.AsNoTracking()
                    on function.Id equals actionInFunction.FunctionId
                join action in _dbContext.Actions.AsNoTracking()
                    on actionInFunction.ActionId equals action.Id
                where action.IsActive != false
                orderby function.SortOrder, function.Name, action.SortOrder, action.Name
                select new
                {
                    function.Id,
                    FunctionName = function.Name,
                    ActionId = action.Id,
                    ActionName = action.Name
                })
                .ToListAsync(cancellationToken);

            return catalog.Select(item => new RolePermissionEntry(
                item.Id,
                item.FunctionName,
                item.ActionId,
                item.ActionName,
                grantedSet.Contains((item.Id, item.ActionId)))).ToArray();
        }

        public async Task<bool> PermissionExistsAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            string functionId,
            string actionId,
            CancellationToken cancellationToken)
        {
            var path = await GetScopePathAsync(tenantId, scopeId, cancellationToken);
            if (path is null) return false;

            return await (
                from assignment in _dbContext.UserRoleAssignments.AsNoTracking()
                where assignment.UserId == userId
                    && ((assignment.ScopeType == IdentityScopeType.Tenant && assignment.ScopeId == path.TenantId)
                        || (path.OrganizationId.HasValue
                            && assignment.ScopeType == IdentityScopeType.Organization
                            && assignment.ScopeId == path.OrganizationId.Value)
                        || (path.BranchId.HasValue
                            && assignment.ScopeType == IdentityScopeType.Branch
                            && assignment.ScopeId == path.BranchId.Value))
                join user in _dbContext.Users.AsNoTracking()
                    on assignment.UserId equals user.Id
                where user.TenantId == tenantId
                join role in _dbContext.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                where role.TenantId == tenantId
                join permission in _dbContext.Permissions.AsNoTracking()
                    on assignment.RoleId equals permission.RoleId
                where permission.FunctionId == functionId
                    && permission.ActionId == actionId
                    && ((permission.ScopeType == IdentityScopeType.Tenant && permission.ScopeId == path.TenantId)
                        || (path.OrganizationId.HasValue
                            && permission.ScopeType == IdentityScopeType.Organization
                            && permission.ScopeId == path.OrganizationId.Value)
                        || (path.BranchId.HasValue
                            && permission.ScopeType == IdentityScopeType.Branch
                            && permission.ScopeId == path.BranchId.Value))
                join function in _dbContext.Functions.AsNoTracking()
                    on permission.FunctionId equals function.Id
                where function.IsActive != false
                join action in _dbContext.Actions.AsNoTracking()
                    on permission.ActionId equals action.Id
                where action.IsActive != false
                join actionInFunction in _dbContext.ActionInFunctions.AsNoTracking()
                    on new { permission.FunctionId, permission.ActionId }
                    equals new { actionInFunction.FunctionId, actionInFunction.ActionId }
                select permission.RoleId)
                .AnyAsync(cancellationToken);
        }

        public async Task<bool> ReplaceRolePermissionsAsync(
            Guid roleId,
            Guid tenantId,
            Guid scopeId,
            IReadOnlyCollection<(string FunctionId, string ActionId)> permissions,
            CancellationToken cancellationToken)
        {
            var path = await GetScopePathAsync(tenantId, scopeId, cancellationToken);
            if (path is null) return false;
            if (!await RoleExistsAsync(roleId, tenantId, cancellationToken))
                return false;

            var scopeType = path.BranchId.HasValue
                ? IdentityScopeType.Branch
                : path.OrganizationId.HasValue
                    ? IdentityScopeType.Organization
                    : IdentityScopeType.Tenant;
            var allowedPairs = await (
                from pair in _dbContext.ActionInFunctions.AsNoTracking()
                join function in _dbContext.Functions.AsNoTracking()
                    on pair.FunctionId equals function.Id
                where function.IsActive != false
                join action in _dbContext.Actions.AsNoTracking()
                    on pair.ActionId equals action.Id
                where action.IsActive != false
                select new { pair.FunctionId, pair.ActionId })
                .ToListAsync(cancellationToken);
            var allowedSet = allowedPairs
                .Select(pair => (pair.FunctionId, pair.ActionId))
                .ToHashSet();

            var normalizedPermissions = permissions
                .Select(permission => (permission.FunctionId.Trim(), permission.ActionId.Trim()))
                .ToHashSet();

            if (normalizedPermissions.Any(permission => !allowedSet.Contains(permission)))
                return false;

            var existing = await _dbContext.Permissions
                .Where(permission => permission.RoleId == roleId
                    && permission.ScopeType == scopeType
                    && permission.ScopeId == scopeId)
                .ToListAsync(cancellationToken);
            var existingSet = existing
                .Select(permission => (permission.FunctionId, permission.ActionId))
                .ToHashSet();
            var permissionsToRemove = existing
                .Where(permission => !normalizedPermissions.Contains((permission.FunctionId, permission.ActionId)))
                .ToArray();
            var permissionsToAdd = normalizedPermissions
                .Where(permission => !existingSet.Contains(permission))
                .ToArray();

            _dbContext.Permissions.RemoveRange(permissionsToRemove);
            _dbContext.Permissions.AddRange(permissionsToAdd.Select(permission => new Permission
            {
                RoleId = roleId,
                FunctionId = permission.Item1,
                ActionId = permission.Item2,
                ScopeType = scopeType,
                ScopeId = scopeId
            }));
            return true;
        }
    }
}
