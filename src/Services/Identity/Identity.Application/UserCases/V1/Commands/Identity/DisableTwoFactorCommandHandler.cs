using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class DisableTwoFactorCommandHandler : ICommandHandler<DisableTwoFactor, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public DisableTwoFactorCommandHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(DisableTwoFactor request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return Result.Failure<bool>(new Error("Identity.TwoFactor.NotEnabled", "Two-factor authentication is not enabled."));

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                EnableTwoFactorCommandHandler.NormalizeCode(request.Code));
            if (!isValid)
                return Result.Failure<bool>(new Error("Identity.TwoFactor.InvalidCode", "The authenticator code is invalid."));

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded)
                return Result.Failure<bool>(new Error("Identity.TwoFactor.DisableFailed", "Could not disable two-factor authentication."));

            await _userManager.ResetAuthenticatorKeyAsync(user);
            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.TwoFactorDisabled,
                SecurityAuditOutcomes.Succeeded,
                user.Id);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}