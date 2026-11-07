using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class CreateAuthenticatorSetupCommandHandler : ICommandHandler<CreateAuthenticatorSetup, Response.AuthenticatorSetup>
    {
        private readonly UserManager<AppUser> _userManager;

        public CreateAuthenticatorSetupCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<Response.AuthenticatorSetup>> Handle(CreateAuthenticatorSetup request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<Response.AuthenticatorSetup>(new Error("Identity.User.NotFound", "The user was not found."));

            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return Result.Failure<Response.AuthenticatorSetup>(new Error("Identity.TwoFactor.AlreadyEnabled", "Two-factor authentication is already enabled."));

            var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(sharedKey))
            {
                var resetResult = await _userManager.ResetAuthenticatorKeyAsync(user);
                if (!resetResult.Succeeded)
                    return Result.Failure<Response.AuthenticatorSetup>(new Error("Identity.TwoFactor.SetupFailed", "Could not create an authenticator key."));

                sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            if (string.IsNullOrEmpty(sharedKey))
                return Result.Failure<Response.AuthenticatorSetup>(new Error("Identity.TwoFactor.SetupFailed", "Could not retrieve an authenticator key."));

            var issuer = "StoreManagement";
            var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var authenticatorUri = $"otpauth://totp/{encodedIssuer}:{account}?secret={sharedKey}&issuer={encodedIssuer}&digits=6";

            return Result.Success(new Response.AuthenticatorSetup(sharedKey, authenticatorUri));
        }
    }
}