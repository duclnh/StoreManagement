using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordReset, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IIdentityEmailSender _emailSender;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

        public RequestPasswordResetCommandHandler(
            UserManager<AppUser> userManager,
            IIdentityEmailSender emailSender,
            ILogger<RequestPasswordResetCommandHandler> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RequestPasswordReset request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || !user.EmailConfirmed)
                return Result.Success(true);

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _emailSender.SendPasswordResetAsync(user.Email!, token, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Could not send a password reset message.");
            }

            return Result.Success(true);
        }
    }
}