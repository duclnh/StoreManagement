using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetUserQueryHandler : IQueryHandler<GetUser, Response.AdminUser>
    {
        private readonly UserManager<AppUser> _userManager;

        public GetUserQueryHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<Response.AdminUser>> Handle(GetUser request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure<Response.AdminUser>(new Error("Identity.User.NotFound", "The user was not found."));

            var roles = await _userManager.GetRolesAsync(user);
            return Result.Success(GetUsersQueryHandler.MapUser(user, roles));
        }
    }
}