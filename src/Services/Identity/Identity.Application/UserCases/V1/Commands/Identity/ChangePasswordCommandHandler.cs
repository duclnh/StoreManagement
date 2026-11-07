using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using MediatR;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePassword, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public ChangePasswordCommandHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ChangePassword request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changeResult.Succeeded)
            {
                var message = string.Join("; ", changeResult.Errors.Select(error => error.Description));
                return Result.Failure<bool>(new Error("Identity.Password.ChangeFailed", message));
            }

            await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);

            return Result.Success(true);
        }
    }
}