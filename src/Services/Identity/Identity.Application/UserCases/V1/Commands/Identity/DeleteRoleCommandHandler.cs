using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRole, bool>
    {
        private const string AdministratorRole = "Administrator";
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public DeleteRoleCommandHandler(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<Result<bool>> Handle(DeleteRole request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role is null)
                return Result.Failure<bool>(new Error("Identity.Role.NotFound", "The role was not found."));

            if (string.Equals(role.Name, AdministratorRole, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<bool>(new Error("Identity.Role.Protected", "The administrator role cannot be deleted."));

            var assignedUsers = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (assignedUsers.Count > 0)
                return Result.Failure<bool>(new Error("Identity.Role.InUse", "Remove the role from all users before deleting it."));

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded
                ? Result.Success(true)
                : Result.Failure<bool>(new Error("Identity.Role.DeleteFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}