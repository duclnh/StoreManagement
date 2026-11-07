using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class CreateManagedUserCommandHandler : ICommandHandler<CreateManagedUser, Guid>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IIdentityEmailSender _emailSender;
        private readonly ILogger<CreateManagedUserCommandHandler> _logger;

        public CreateManagedUserCommandHandler(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IIdentityEmailSender emailSender,
            ILogger<CreateManagedUserCommandHandler> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateManagedUser request, CancellationToken cancellationToken)
        {
            var roles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    return Result.Failure<Guid>(new Error("Identity.Role.NotFound", $"The role '{role}' does not exist."));
            }

            var email = request.Email.Trim();
            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
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
            }

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
