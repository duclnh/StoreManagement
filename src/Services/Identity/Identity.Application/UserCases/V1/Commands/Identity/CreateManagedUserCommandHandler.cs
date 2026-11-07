using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class CreateManagedUserCommandHandler : ICommandHandler<CreateManagedUser, Guid>
    {
        private const string AdministratorRole = "Administrator";
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IIdentityEmailSender _emailSender;
        private readonly ILogger<CreateManagedUserCommandHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public CreateManagedUserCommandHandler(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IIdentityEmailSender emailSender,
            ILogger<CreateManagedUserCommandHandler> logger,
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<Guid>> Handle(CreateManagedUser request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<Guid>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var roles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var role in roles)
            {
                if (string.Equals(role, AdministratorRole, StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<Guid>(new Error(
                        "Identity.User.PrivilegedRoleAssignmentRequiresAdministration",
                        "The Administrator role cannot be assigned during managed account creation."));
                }

                var existingRole = await _roleManager.FindByNameAsync(role);
                if (existingRole is null || existingRole.TenantId != tenantId)
                    return Result.Failure<Guid>(new Error("Identity.Role.NotFound", $"The role '{role}' does not exist."));
            }

            var email = request.Email.Trim();
            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                FullName = $"{firstName} {lastName}"
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                return Result.Failure<Guid>(new Error("Identity.User.CreateFailed", string.Join("; ", createResult.Errors.Select(error => error.Description))));

            if (roles.Length > 0)
            {
                var roleResult = await _userManager.AddToRolesAsync(user, roles);
                if (!roleResult.Succeeded)
                {
                    var deleteResult = await _userManager.DeleteAsync(user);
                    var message = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                    if (!deleteResult.Succeeded)
                        message += $"; account cleanup failed: {string.Join("; ", deleteResult.Errors.Select(error => error.Description))}";
                    return Result.Failure<Guid>(new Error("Identity.User.RoleUpdateFailed", message));
                }

                var roleIds = new List<Guid>(roles.Length);
                foreach (var roleName in roles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role is null || role.TenantId != tenantId)
                    {
                        await _userManager.DeleteAsync(user);
                        return Result.Failure<Guid>(new Error("Identity.Role.NotFound", $"The role '{roleName}' does not exist."));
                    }
                    roleIds.Add(role.Id);
                }

                if (!await _unitOfWork.UserRoleAssignmentRepository.ReplaceRoleIdsAtScopeAsync(
                        user.Id,
                        tenantId,
                        tenantId,
                        roleIds,
                        cancellationToken))
                {
                    var deleteResult = await _userManager.DeleteAsync(user);
                    var message = "Could not assign the requested roles at tenant scope.";
                    if (!deleteResult.Succeeded)
                        message += $" Account cleanup failed: {string.Join("; ", deleteResult.Errors.Select(error => error.Description))}";
                    return Result.Failure<Guid>(new Error("Identity.User.RoleUpdateFailed", message));
                }
            }

            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.UserCreated,
                SecurityAuditOutcomes.Succeeded,
                user.Id,
                $"RoleCount:{roles.Length}");
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailSender.SendEmailConfirmationAsync(email, token, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Could not send an email confirmation message for managed user {UserId}.", user.Id);
            }

            return Result.Success(user.Id);
        }
    }
}
