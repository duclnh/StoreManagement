using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmail, bool>
    {
        private readonly UserManager<AppUser> _userManager;

        public ConfirmEmailCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<bool>> Handle(ConfirmEmail request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return InvalidConfirmation();

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            return result.Succeeded ? Result.Success(true) : InvalidConfirmation();
        }

        private static Result<bool> InvalidConfirmation() =>
            Result.Failure<bool>(new Error("Identity.Email.InvalidConfirmation", "The email confirmation token is invalid or expired."));
    }
}