using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetCurrentUserProfileQueryHandler
        : IQueryHandler<GetCurrentUserProfile, Response.CurrentUserProfile>
    {
        private readonly UserManager<AppUser> _userManager;

        public GetCurrentUserProfileQueryHandler(UserManager<AppUser> userManager) =>
            _userManager = userManager;

        public async Task<Result<Response.CurrentUserProfile>> Handle(
            GetCurrentUserProfile request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null || !user.IsActive)
                return Result.Failure<Response.CurrentUserProfile>(new Error(
                    "Identity.User.NotFound",
                    "The account was not found."));

            var isAdministrator = await _userManager.IsInRoleAsync(user, "Administrator");
            var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            return Result.Success(new Response.CurrentUserProfile(
                user.Id,
                user.TenantId,
                isAdministrator,
                twoFactorEnabled));
        }
    }
}
