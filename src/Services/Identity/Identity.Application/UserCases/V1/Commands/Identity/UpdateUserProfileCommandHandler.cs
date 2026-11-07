using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfile, bool>
    {
        private readonly UserManager<AppUser> _userManager;

        public UpdateUserProfileCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<bool>> Handle(UpdateUserProfile request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}";

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? Result.Success(true)
                : Result.Failure<bool>(new Error("Identity.User.UpdateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}