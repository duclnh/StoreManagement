using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class UserBelongsToTenantQueryHandler : IQueryHandler<UserBelongsToTenantQuery, bool>
    {
        private readonly UserManager<AppUser> _users;

        public UserBelongsToTenantQueryHandler(UserManager<AppUser> users)
        {
            _users = users;
        }

        public async Task<Result<bool>> Handle(UserBelongsToTenantQuery request, CancellationToken cancellationToken)
        {
            var user = await _users.FindByIdAsync(request.UserId.ToString());
            return Result.Success(user is not null && user.TenantId == request.TenantId && user.IsActive
                && (!user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow));
        }
    }
}
