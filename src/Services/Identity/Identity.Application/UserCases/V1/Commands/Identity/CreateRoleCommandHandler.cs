using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRole, Guid>
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public CreateRoleCommandHandler(
            RoleManager<AppRole> roleManager,
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<Guid>> Handle(CreateRole request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<Guid>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            if (await _roleManager.RoleExistsAsync(request.Name))
                return Result.Failure<Guid>(new Error("Identity.Role.AlreadyExists", "A role with this name already exists."));

            var role = new AppRole
            {
                TenantId = tenantId,
                Name = request.Name.Trim(),
                RoleCode = request.RoleCode.Trim(),
                Description = request.Description.Trim()
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.RoleCreated,
                    SecurityAuditOutcomes.Succeeded,
                    details: $"Role:{role.Id}");
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            return result.Succeeded
                ? Result.Success(role.Id)
                : Result.Failure<Guid>(new Error("Identity.Role.CreateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}