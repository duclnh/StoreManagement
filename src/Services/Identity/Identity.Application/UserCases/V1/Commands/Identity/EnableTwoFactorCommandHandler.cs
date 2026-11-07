using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class EnableTwoFactorCommandHandler : ICommandHandler<EnableTwoFactor, bool>
    {
        private readonly UserManager<AppUser> _userManager;

        public EnableTwoFactorCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<bool>> Handle(EnableTwoFactor request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            var code = NormalizeCode(request.Code);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                code);
            if (!isValid)
                return Result.Failure<bool>(new Error("Identity.TwoFactor.InvalidCode", "The authenticator code is invalid."));

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
                return Result.Failure<bool>(new Error("Identity.TwoFactor.EnableFailed", "Could not enable two-factor authentication."));

            return Result.Success(true);
        }

        internal static string NormalizeCode(string code) => code.Replace(" ", string.Empty).Replace("-", string.Empty);
    }
}