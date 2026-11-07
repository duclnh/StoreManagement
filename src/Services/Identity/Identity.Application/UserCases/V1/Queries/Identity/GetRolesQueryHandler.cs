using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
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

        public GetRolesQueryHandler(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<Result<List<Response.AdminRole>>> Handle(GetRoles request, CancellationToken cancellationToken)
        {
            var roles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .ToListAsync(cancellationToken);

            var results = new List<Response.AdminRole>(roles.Count);
            foreach (var role in roles)
            {
                var userCount = role.Name is null
                    ? 0
                    : (await _userManager.GetUsersInRoleAsync(role.Name)).Count;
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