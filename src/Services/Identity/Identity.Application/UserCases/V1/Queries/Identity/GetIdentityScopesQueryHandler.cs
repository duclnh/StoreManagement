using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Contract.Services.V1.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetIdentityScopesQueryHandler : IQueryHandler<GetIdentityScopes, List<Response.IdentityScope>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public GetIdentityScopesQueryHandler(IUnitOfWork unitOfWork, IIdentityTenantContext tenantContext)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<List<Response.IdentityScope>>> Handle(
            GetIdentityScopes request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId != request.TenantId)
                return Result.Failure<List<Response.IdentityScope>>(new Error(
                    "Identity.Tenant.Invalid",
                    "The requested tenant does not match the authenticated tenant."));

            var scopes = await _unitOfWork.RolePermissionRepository
                .GetScopesAsync(request.TenantId, cancellationToken);
            return Result.Success(scopes
                .Select(scope => new Response.IdentityScope(scope.Id, scope.Name, scope.ScopeType, scope.ParentId))
                .ToList());
        }
    }
}
