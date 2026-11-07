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
        private readonly IIdentityTenantContext _tenantContext;

        public GetUsersQueryHandler(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IIdentityTenantContext tenantContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        public async Task<Result<Response.UserPage>> Handle(GetUsers request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<Response.UserPage>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
            var totalCount = await _unitOfWork.AppUserRepository.CountAsync(tenantId, search, cancellationToken);
            var users = await _unitOfWork.AppUserRepository.GetPageAsync(
                tenantId,
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