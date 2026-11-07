using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class AssignUserRolesCommandHandler : ICommandHandler<AssignUserRoles, bool>
    {
        private const string AdministratorRole = "Administrator";
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public AssignUserRolesCommandHandler(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(AssignUserRoles request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            var desiredRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var roleName in desiredRoles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    return Result.Failure<bool>(new Error("Identity.Role.NotFound", $"The role '{roleName}' does not exist."));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removesAdministrator = currentRoles.Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase)
                && !desiredRoles.Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase);
            if (removesAdministrator)
            {
                if (request.ActorId == user.Id)
                    return Result.Failure<bool>(new Error("Identity.User.CannotRemoveOwnAdminRole", "You cannot remove your own administrator role."));

                var administrators = await _userManager.GetUsersInRoleAsync(AdministratorRole);
                if (user.IsActive && administrators.Count(admin => admin.IsActive) <= 1)
                    return Result.Failure<bool>(new Error("Identity.User.LastAdministrator", "The last active administrator cannot lose the administrator role."));
            }

            var rolesToRemove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesToAdd = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesChanged = false;

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                    return Result.Failure<bool>(new Error("Identity.User.RoleUpdateFailed", string.Join("; ", removeResult.Errors.Select(error => error.Description))));
                rolesChanged = true;
            }

            if (rolesToAdd.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    if (rolesChanged)
                        await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
                    return Result.Failure<bool>(new Error("Identity.User.RoleUpdateFailed", string.Join("; ", addResult.Errors.Select(error => error.Description))));
                }
                rolesChanged = true;
            }

            if (rolesChanged)
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);

            return Result.Success(true);
        }
    }
}