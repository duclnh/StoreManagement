using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Repositories;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetIdentityAdminAccessQueryHandler
        : IQueryHandler<GetIdentityAdminAccess, Response.IdentityAdminAccess>
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IIdentityTenantContext _tenantContext;

        public GetIdentityAdminAccessQueryHandler(
            IRolePermissionRepository rolePermissionRepository,
            IIdentityTenantContext tenantContext)
        {
            _rolePermissionRepository = rolePermissionRepository;
            _tenantContext = tenantContext;
        }

        public async Task<Result<Response.IdentityAdminAccess>> Handle(
            GetIdentityAdminAccess request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId != request.TenantId
                || _tenantContext.UserId != request.UserId)
                return Result.Failure<Response.IdentityAdminAccess>(new Error(
                    "Identity.Tenant.Invalid",
                    "The requested tenant does not match the authenticated account."));

            if (await _rolePermissionRepository.GetScopePathAsync(
                    request.TenantId,
                    request.ScopeId,
                    cancellationToken) is null)
                return Result.Failure<Response.IdentityAdminAccess>(new Error(
                    "Identity.Scope.NotFound",
                    "The requested scope was not found in the tenant."));

            var isAdministrator = _tenantContext.IsPlatformAdministrator;
            var grants = isAdministrator
                ? new HashSet<(string FunctionId, string ActionId)>()
                : (await _rolePermissionRepository.GetGrantedPermissionsAsync(
                    request.UserId,
                    request.TenantId,
                    request.ScopeId,
                    cancellationToken)).ToHashSet();

            bool HasGrant(string functionId, string actionId) =>
                isAdministrator || grants.Contains((functionId, actionId));
            var tenantScope = request.ScopeId == request.TenantId;

            return Result.Success(new Response.IdentityAdminAccess(
                request.TenantId,
                request.ScopeId,
                isAdministrator,
                HasGrant("Identity.Users", "View"),
                HasGrant("Identity.Users", "Create") && (tenantScope || HasGrant("Identity.Users", "Assign")),
                tenantScope && HasGrant("Identity.Users", "Update"),
                HasGrant("Identity.Users", "Assign"),
                HasGrant("Identity.Roles", "View"),
                tenantScope && HasGrant("Identity.Roles", "Create"),
                tenantScope && HasGrant("Identity.Roles", "Update"),
                tenantScope && HasGrant("Identity.Roles", "Delete"),
                HasGrant("Identity.Roles", "Assign"),
                HasGrant("Identity.Security", "View"),
                HasGrant("Identity.Sessions", "View"),
                HasGrant("Identity.Sessions", "Revoke")));
        }
    }
}
