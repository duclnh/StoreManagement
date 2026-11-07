using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfile, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public UpdateUserProfileCommandHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<bool>> Handle(UpdateUserProfile request, CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId is not Guid tenantId)
                return Result.Failure<bool>(new Error("Identity.Tenant.Required", "A tenant context is required."));

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null || user.TenantId != tenantId)
                return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}";

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.UserUpdated,
                    SecurityAuditOutcomes.Succeeded,
                    user.Id);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            return result.Succeeded
                ? Result.Success(true)
                : Result.Failure<bool>(new Error("Identity.User.UpdateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}