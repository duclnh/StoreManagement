using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetRolesQueryHandler : IQueryHandler<GetRoles, List<Response.AdminRole>>
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IIdentityTenantContext _tenantContext;

        public GetRolesQueryHandler(
            RoleManager<AppRole> roleManager,
            UserManager<AppUser> userManager,
            IIdentityTenantContext tenantContext)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        public async Task<Result<List<Response.AdminRole>>> Handle(GetRoles request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<List<Response.AdminRole>>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var roles = await _roleManager.Roles
                .Where(role => role.TenantId == tenantId)
                .OrderBy(role => role.Name)
                .ToListAsync(cancellationToken);

            var results = new List<Response.AdminRole>(roles.Count);
            foreach (var role in roles)
            {
                var userCount = role.Name is null
                    ? 0
                    : (await _userManager.GetUsersInRoleAsync(role.Name))
                        .Count(user => user.TenantId == tenantId);
                results.Add(new Response.AdminRole(
                    role.Id,
                    role.Name ?? string.Empty,
                    role.RoleCode,
                    role.Description,
                    userCount));
            }

            return Result.Success(results);
        }
    }
}