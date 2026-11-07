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
            if (user is null || user.TenantId != request.TenantId)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            var desiredRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var roleName in desiredRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null || role.TenantId != request.TenantId)
                    return Result.Failure<bool>(new Error("Identity.Role.NotFound", $"The role '{roleName}' does not exist."));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (desiredRoles.Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase)
                || currentRoles.Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase))
            {
                var actor = await _userManager.FindByIdAsync(request.ActorId.ToString());
                if (actor is null
                    || (actor.TenantId != request.TenantId && !await _userManager.IsInRoleAsync(actor, AdministratorRole))
                    || !await _userManager.IsInRoleAsync(actor, AdministratorRole))
                    return Result.Failure<bool>(new Error(
                        "Identity.User.PrivilegedRoleAssignmentRequiresAdministration",
                        "Only an administrator can change Administrator role membership."));

                if (request.ScopeId != request.TenantId)
                    return Result.Failure<bool>(new Error(
                        "Identity.User.AdministratorRoleTenantScopeOnly",
                        "The Administrator role can only be assigned at tenant scope."));
            }

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

            var desiredRoleIds = new List<Guid>();
            foreach (var roleName in desiredRoles.Where(role => !string.Equals(
                         role,
                         AdministratorRole,
                         StringComparison.OrdinalIgnoreCase)))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null || role.TenantId != request.TenantId)
                    return Result.Failure<bool>(new Error("Identity.Role.NotFound", $"The role '{roleName}' does not exist."));
                desiredRoleIds.Add(role.Id);
            }

            var currentRoleIdsAtScope = await _unitOfWork.UserRoleAssignmentRepository
                .GetRoleIdsAtScopeAsync(user.Id, request.TenantId, request.ScopeId, cancellationToken);
            var allCurrentRoleIds = await _unitOfWork.UserRoleAssignmentRepository
                .GetAllRoleIdsAsync(user.Id, request.TenantId, cancellationToken);
            var assignmentChanged = !currentRoleIdsAtScope.ToHashSet().SetEquals(desiredRoleIds);
            var allDesiredRoleIds = allCurrentRoleIds
                .Except(currentRoleIdsAtScope)
                .Concat(desiredRoleIds)
                .Distinct()
                .ToArray();

            if (!await _unitOfWork.UserRoleAssignmentRepository.ReplaceRoleIdsAtScopeAsync(
                    user.Id,
                    request.TenantId,
                    request.ScopeId,
                    desiredRoleIds,
                    cancellationToken))
                return Result.Failure<bool>(new Error("Identity.Scope.Invalid", "The selected scope is invalid for this user."));

            var desiredMembershipRoles = new List<string>();
            foreach (var roleId in allDesiredRoleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role?.Name is not null) desiredMembershipRoles.Add(role.Name);
            }
            if (desiredRoles.Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase))
                desiredMembershipRoles.Add(AdministratorRole);

            var rolesToRemove = currentRoles.Except(desiredMembershipRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesToAdd = desiredMembershipRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
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

            if (rolesChanged || assignmentChanged)
            {
                if (rolesChanged)
                    await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
                _unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.UserRolesChanged,
                    SecurityAuditOutcomes.Succeeded,
                    user.Id,
                    $"RoleCount:{desiredRoles.Length}");
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }

            return Result.Success(true);
        }
    }
}