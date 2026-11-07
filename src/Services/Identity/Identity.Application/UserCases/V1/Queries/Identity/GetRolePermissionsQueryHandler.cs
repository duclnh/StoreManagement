using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Contract.Services.V1.Identity;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetRolePermissionsQueryHandler : IQueryHandler<GetRolePermissions, List<Response.RolePermission>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRolePermissionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<Response.RolePermission>>> Handle(
            GetRolePermissions request,
            CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.RolePermissionRepository;
            if (!await repository.RoleExistsAsync(request.RoleId, request.TenantId, cancellationToken))
            {
                return Result.Failure<List<Response.RolePermission>>(new Error(
                    "Identity.Role.NotFound",
                    "The role was not found."));
            }

            if (await repository.GetScopePathAsync(request.TenantId, request.ScopeId, cancellationToken) is null)
            {
                return Result.Failure<List<Response.RolePermission>>(new Error(
                    "Identity.Scope.NotFound",
                    "The requested scope was not found in the tenant."));
            }

            var permissions = await repository.GetRolePermissionsAsync(
                request.RoleId,
                request.TenantId,
                request.ScopeId,
                cancellationToken);
            return Result.Success(permissions
                .Select(permission => new Response.RolePermission(
                    permission.FunctionId,
                    permission.FunctionName,
                    permission.ActionId,
                    permission.ActionName,
                    permission.IsGranted))
                .ToList());
        }
    }
}
