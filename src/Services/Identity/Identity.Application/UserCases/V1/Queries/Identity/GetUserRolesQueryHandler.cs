using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetUserRolesQueryHandler : IQueryHandler<GetUserRoles, List<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public GetUserRolesQueryHandler(
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<List<string>>> Handle(
            GetUserRoles request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId != request.TenantId)
                return Result.Failure<List<string>>(new Error(
                    "Identity.Tenant.Invalid",
                    "The requested tenant does not match the authenticated tenant."));

            if (await _unitOfWork.RolePermissionRepository.GetScopePathAsync(
                    request.TenantId,
                    request.ScopeId,
                    cancellationToken) is null)
                return Result.Failure<List<string>>(new Error(
                    "Identity.Scope.NotFound",
                    "The requested scope was not found in the tenant."));

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null || user.TenantId != request.TenantId)
                return Result.Failure<List<string>>(new Error("Identity.User.NotFound", "The user was not found."));

            var roleIds = await _unitOfWork.UserRoleAssignmentRepository.GetRoleIdsAtScopeAsync(
                user.Id,
                request.TenantId,
                request.ScopeId,
                cancellationToken);
            var roleNames = new List<string>(roleIds.Count);
            foreach (var roleId in roleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role?.TenantId == request.TenantId && role.Name is not null)
                    roleNames.Add(role.Name);
            }

            return Result.Success(roleNames);
        }
    }
}
