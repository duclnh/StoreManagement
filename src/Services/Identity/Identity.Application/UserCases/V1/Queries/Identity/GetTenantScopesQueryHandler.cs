using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetTenantScopesQueryHandler : IQueryHandler<GetTenantScopes, List<Response.IdentityAdminScope>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTenantScopesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Result<List<Response.IdentityAdminScope>>> Handle(
            GetTenantScopes request,
            CancellationToken cancellationToken)
        {
            var scopes = await _unitOfWork.IdentityScopeRepository.GetTenantScopesAsync(
                request.TenantId,
                cancellationToken);
            return Result.Success(scopes
                .Select(scope => new Response.IdentityAdminScope(
                    scope.Id,
                    scope.Name,
                    scope.ScopeType,
                    scope.ParentId,
                    scope.IsActive))
                .ToList());
        }
    }
}
