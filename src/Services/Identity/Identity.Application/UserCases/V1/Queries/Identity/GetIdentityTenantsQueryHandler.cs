using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Contract.Services.V1.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetIdentityTenantsQueryHandler :
        IQueryHandler<GetIdentityTenants, List<Response.IdentityTenant>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetIdentityTenantsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Result<List<Response.IdentityTenant>>> Handle(
            GetIdentityTenants request,
            CancellationToken cancellationToken)
        {
            var tenants = await _unitOfWork.IdentityScopeRepository.GetTenantsAsync(cancellationToken);
            return Result.Success(tenants
                .Select(tenant => new Response.IdentityTenant(
                    tenant.Id,
                    tenant.Name,
                    tenant.IsActive,
                    tenant.CreatedAtUtc))
                .ToList());
        }
    }
}
