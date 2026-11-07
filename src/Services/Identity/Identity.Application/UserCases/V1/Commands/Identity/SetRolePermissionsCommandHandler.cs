using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class SetRolePermissionsCommandHandler : ICommandHandler<SetRolePermissionsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SetRolePermissionsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.RolePermissionRepository;
            if (!await repository.RoleExistsAsync(request.RoleId, request.TenantId, cancellationToken))
            {
                return Result.Failure<bool>(new Error(
                    "Identity.Role.NotFound",
                    "The role was not found."));
            }

            var permissions = request.Permissions
                .Select(permission => (permission.FunctionId, permission.ActionId))
                .ToArray();
            if (!await repository.ReplaceRolePermissionsAsync(
                    request.RoleId,
                    request.TenantId,
                    request.ScopeId,
                    permissions,
                    cancellationToken))
            {
                return Result.Failure<bool>(new Error(
                    "Identity.Permission.Unsupported",
                    "One or more function/action pairs are not supported."));
            }

            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.RolePermissionsChanged,
                SecurityAuditOutcomes.Succeeded,
                details: $"Role:{request.RoleId};Scope:{request.ScopeId};GrantCount:{permissions.Length}",
                tenantId: request.TenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
