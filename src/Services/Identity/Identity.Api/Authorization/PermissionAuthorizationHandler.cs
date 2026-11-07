using Identity.Domain.Abstractions;
using Identity.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Identity.Api.Authorization
{
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IRolePermissionRepository _permissions;
        private readonly IIdentityTenantContext _tenantContext;

        public PermissionAuthorizationHandler(
            IRolePermissionRepository permissions,
            IIdentityTenantContext tenantContext)
        {
            _permissions = permissions;
            _tenantContext = tenantContext;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User.IsInRole("Administrator"))
            {
                context.Succeed(requirement);
                return;
            }

            if (!_tenantContext.UserId.HasValue || !_tenantContext.TenantId.HasValue
                || !_tenantContext.RequestedScopeId.HasValue)
                return;

            if (await _permissions.PermissionExistsAsync(
                _tenantContext.UserId.Value,
                _tenantContext.TenantId.Value,
                _tenantContext.RequestedScopeId.Value,
                requirement.FunctionId,
                requirement.ActionId,
                context.Resource is HttpContext httpContext
                    ? httpContext.RequestAborted
                    : CancellationToken.None))
            {
                context.Succeed(requirement);
            }
        }
    }

    public static class PermissionAuthorizationExtensions
    {
        public static RouteHandlerBuilder RequirePermission(
            this RouteHandlerBuilder endpoint,
            string functionId,
            string actionId)
        {
            return endpoint.RequireAuthorization(policy =>
                policy.AddRequirements(new PermissionRequirement(functionId, actionId)));
        }
    }
}
