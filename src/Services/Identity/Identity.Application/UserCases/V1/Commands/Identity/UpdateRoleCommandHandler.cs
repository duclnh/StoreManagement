using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRole, bool>
    {
        private const string AdministratorRole = "Administrator";
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public UpdateRoleCommandHandler(
            RoleManager<AppRole> roleManager,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<bool>> Handle(UpdateRole request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<bool>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role is null || role.TenantId != tenantId)
                return Result.Failure<bool>(new Error("Identity.Role.NotFound", "The role was not found."));

            var existingRole = await _roleManager.FindByNameAsync(request.Name.Trim());
            if (existingRole is not null && existingRole.Id != role.Id)
                return Result.Failure<bool>(new Error("Identity.Role.AlreadyExists", "A role with this name already exists."));

            var newName = request.Name.Trim();
            var nameChanged = !string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase);
            if (nameChanged && string.Equals(role.Name, AdministratorRole, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<bool>(new Error("Identity.Role.Protected", "The administrator role name cannot be changed."));

            var assignedUsers = nameChanged
                ? await _userManager.GetUsersInRoleAsync(role.Name!)
                : [];

            role.Name = newName;
            role.RoleCode = request.RoleCode.Trim();
            role.Description = request.Description.Trim();

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return Result.Failure<bool>(new Error("Identity.Role.UpdateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));

            foreach (var user in assignedUsers)
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);

            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.RoleUpdated,
                SecurityAuditOutcomes.Succeeded,
                details: $"Role:{role.Id}");
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}