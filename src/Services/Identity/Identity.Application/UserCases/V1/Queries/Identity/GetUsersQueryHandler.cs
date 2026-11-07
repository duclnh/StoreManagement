using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetUsersQueryHandler : IQueryHandler<GetUsers, Response.UserPage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public GetUsersQueryHandler(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<Response.UserPage>> Handle(GetUsers request, CancellationToken cancellationToken)
        {
            var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
            var totalCount = await _unitOfWork.AppUserRepository.CountAsync(search, cancellationToken);
            var users = await _unitOfWork.AppUserRepository.GetPageAsync(
                search,
                (request.Page - 1) * request.PageSize,
                request.PageSize,
                cancellationToken);

            var items = new List<Response.AdminUser>(users.Count);
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(MapUser(user, roles));
            }

            return Result.Success(new Response.UserPage(items, request.Page, request.PageSize, totalCount));
        }

        internal static Response.AdminUser MapUser(AppUser user, IEnumerable<string> roles) =>
            new(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.IsActive,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                roles.OrderBy(role => role).ToArray());
    }
}