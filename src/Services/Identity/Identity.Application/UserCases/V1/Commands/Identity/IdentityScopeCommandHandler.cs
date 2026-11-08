using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class IdentityScopeCommandHandler :
        ICommandHandler<CreateIdentityTenant, Response.IdentityScope>,
        ICommandHandler<CreateIdentityOrganization, Response.IdentityScope>,
        ICommandHandler<CreateIdentityBranch, Response.IdentityScope>,
        ICommandHandler<SetIdentityTenantStatus, bool>,
        ICommandHandler<SetIdentityOrganizationStatus, bool>,
        ICommandHandler<SetIdentityBranchStatus, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public IdentityScopeCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public Task<Result<Response.IdentityScope>> Handle(
            CreateIdentityTenant request,
            CancellationToken cancellationToken) =>
            CreateScopeAsync(
                ct => _unitOfWork.IdentityScopeRepository.CreateTenantAsync(request.Name.Trim(), ct),
                request.Name.Trim(),
                (int)IdentityScopeType.Tenant,
                null,
                null,
                cancellationToken);

        public Task<Result<Response.IdentityScope>> Handle(
            CreateIdentityOrganization request,
            CancellationToken cancellationToken) =>
            CreateScopeAsync(
                ct => _unitOfWork.IdentityScopeRepository.CreateOrganizationAsync(
                    request.TenantId,
                    request.Name.Trim(),
                    ct),
                request.Name.Trim(),
                (int)IdentityScopeType.Organization,
                request.TenantId,
                request.TenantId,
                cancellationToken);

        public Task<Result<Response.IdentityScope>> Handle(
            CreateIdentityBranch request,
            CancellationToken cancellationToken) =>
            CreateScopeAsync(
                ct => _unitOfWork.IdentityScopeRepository.CreateBranchAsync(
                    request.TenantId,
                    request.OrganizationId,
                    request.Name.Trim(),
                    ct),
                request.Name.Trim(),
                (int)IdentityScopeType.Branch,
                request.OrganizationId,
                request.TenantId,
                cancellationToken);

        public Task<Result<bool>> Handle(
            SetIdentityTenantStatus request,
            CancellationToken cancellationToken) =>
            SetStatusAsync(
                ct => _unitOfWork.IdentityScopeRepository.SetTenantActiveAsync(
                    request.TenantId,
                    request.IsActive,
                    ct),
                request.TenantId,
                request.TenantId,
                "Tenant",
                request.IsActive,
                cancellationToken);

        public Task<Result<bool>> Handle(
            SetIdentityOrganizationStatus request,
            CancellationToken cancellationToken) =>
            SetStatusAsync(
                ct => _unitOfWork.IdentityScopeRepository.SetOrganizationActiveAsync(
                    request.TenantId,
                    request.OrganizationId,
                    request.IsActive,
                    ct),
                request.OrganizationId,
                request.TenantId,
                "Organization",
                request.IsActive,
                cancellationToken);

        public Task<Result<bool>> Handle(
            SetIdentityBranchStatus request,
            CancellationToken cancellationToken) =>
            SetStatusAsync(
                ct => _unitOfWork.IdentityScopeRepository.SetBranchActiveAsync(
                    request.TenantId,
                    request.OrganizationId,
                    request.BranchId,
                    request.IsActive,
                    ct),
                request.BranchId,
                request.TenantId,
                "Branch",
                request.IsActive,
                cancellationToken);

        private async Task<Result<Response.IdentityScope>> CreateScopeAsync(
            Func<CancellationToken, Task<IdentityScopeMutationResult>> create,
            string name,
            int scopeType,
            Guid? parentId,
            Guid? tenantId,
            CancellationToken cancellationToken)
        {
            var result = await create(cancellationToken);
            if (result.Status != IdentityScopeMutationStatus.Succeeded || !result.ScopeId.HasValue)
                return ScopeFailure<Response.IdentityScope>(result.Status);

            var scopeId = result.ScopeId.Value;
            var auditTenantId = tenantId ?? scopeId;
            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.ScopeCreated,
                SecurityAuditOutcomes.Succeeded,
                details: $"Type:{scopeType};Scope:{scopeId}",
                tenantId: auditTenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result.Success(new Response.IdentityScope(scopeId, name, scopeType, parentId));
        }

        private async Task<Result<bool>> SetStatusAsync(
            Func<CancellationToken, Task<IdentityScopeMutationStatus>> setStatus,
            Guid scopeId,
            Guid tenantId,
            string scopeType,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var status = await setStatus(cancellationToken);
            if (status != IdentityScopeMutationStatus.Succeeded)
                return ScopeFailure<bool>(status);

            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.ScopeStatusChanged,
                SecurityAuditOutcomes.Succeeded,
                details: $"Type:{scopeType};Scope:{scopeId};IsActive:{isActive}",
                tenantId: tenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Success(true);
        }

        private static Result<T> ScopeFailure<T>(IdentityScopeMutationStatus status) =>
            status switch
            {
                IdentityScopeMutationStatus.NotFound => Result.Failure<T>(new Error(
                    "Identity.Scope.NotFound",
                    "The parent scope is unavailable or the requested scope was not found.")),
                IdentityScopeMutationStatus.Conflict => Result.Failure<T>(new Error(
                    "Identity.Scope.Conflict",
                    "A scope with the same name already exists under this parent.")),
                _ => Result.Failure<T>(new Error(
                    "Identity.Scope.Invalid",
                    "The identity scope operation could not be completed."))
            };
    }
}
