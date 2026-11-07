using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class RequestEmailConfirmationCommandHandler : ICommandHandler<RequestEmailConfirmation, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IIdentityEmailSender _emailSender;
        private readonly ILogger<RequestEmailConfirmationCommandHandler> _logger;

        public RequestEmailConfirmationCommandHandler(
            UserManager<AppUser> userManager,
            IIdentityEmailSender emailSender,
            ILogger<RequestEmailConfirmationCommandHandler> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RequestEmailConfirmation request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || user.EmailConfirmed)
                return Result.Success(true);

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailSender.SendEmailConfirmationAsync(user.Email!, token, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Could not send an email confirmation message.");
            }

            return Result.Success(true);
        }
    }
}