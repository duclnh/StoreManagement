using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPassword, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public ResetPasswordCommandHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ResetPassword request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return InvalidReset();

            var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!resetResult.Succeeded)
                return InvalidReset();

            await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
            return Result.Success(true);
        }

        private static Result<bool> InvalidReset() =>
            Result.Failure<bool>(new Error("Identity.Password.InvalidReset", "The reset token is invalid or expired, or the password does not meet policy."));
    }
}