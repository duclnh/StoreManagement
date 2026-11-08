using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Contract.Services.V1.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetIdentityScopeQueryHandler :
        IQueryHandler<GetIdentityScope, Response.IdentityScope?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetIdentityScopeQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Result<Response.IdentityScope?>> Handle(
            GetIdentityScope request,
            CancellationToken cancellationToken)
        {
            var scope = await _unitOfWork.IdentityScopeRepository.GetScopeAsync(
                request.ScopeId,
                cancellationToken);
            return Result.Success(scope is null
                ? null
                : new Response.IdentityScope(scope.Id, scope.Name, scope.ScopeType, scope.ParentId));
        }
    }
}
