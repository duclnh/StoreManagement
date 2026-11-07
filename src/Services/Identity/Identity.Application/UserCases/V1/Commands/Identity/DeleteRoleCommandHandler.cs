using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public DeleteRoleCommandHandler(
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

        public async Task<Result<bool>> Handle(DeleteRole request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<bool>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role is null || role.TenantId != tenantId)
                return Result.Failure<bool>(new Error("Identity.Role.NotFound", "The role was not found."));

            if (string.Equals(role.Name, AdministratorRole, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<bool>(new Error("Identity.Role.Protected", "The administrator role cannot be deleted."));

            var assignedUsers = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (assignedUsers.Count > 0)
                return Result.Failure<bool>(new Error("Identity.Role.InUse", "Remove the role from all users before deleting it."));

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                _unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.RoleDeleted,
                    SecurityAuditOutcomes.Succeeded,
                    details: $"Role:{role.Id}");
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            return result.Succeeded
                ? Result.Success(true)
                : Result.Failure<bool>(new Error("Identity.Role.DeleteFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}