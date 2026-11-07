using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class SetUserActiveCommandHandler : ICommandHandler<SetUserActive, bool>
    {
        private const string AdministratorRole = "Administrator";
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public SetUserActiveCommandHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(SetUserActive request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            if (!request.IsActive && request.ActorId == user.Id)
                return Result.Failure<bool>(new Error("Identity.User.CannotDeactivateSelf", "You cannot deactivate your own account."));

            if (!request.IsActive && await _userManager.IsInRoleAsync(user, AdministratorRole))
            {
                var administrators = await _userManager.GetUsersInRoleAsync(AdministratorRole);
                if (administrators.Count(admin => admin.IsActive) <= 1)
                    return Result.Failure<bool>(new Error("Identity.User.LastAdministrator", "The last active administrator cannot be deactivated."));
            }

            user.IsActive = request.IsActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result.Failure<bool>(new Error("Identity.User.UpdateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));

            if (!request.IsActive)
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);

            return Result.Success(true);
        }
    }
}