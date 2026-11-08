using FluentValidation;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Contract.Services.V1.Identity.Validators
{
    public sealed class CreateIdentityTenantValidator : AbstractValidator<CreateIdentityTenant>
    {
        public CreateIdentityTenantValidator() => RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
    }

    public sealed class CreateIdentityOrganizationValidator : AbstractValidator<CreateIdentityOrganization>
    {
        public CreateIdentityOrganizationValidator()
        {
            RuleFor(request => request.TenantId).NotEmpty();
            RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        }
    }

    public sealed class CreateIdentityBranchValidator : AbstractValidator<CreateIdentityBranch>
    {
        public CreateIdentityBranchValidator()
        {
            RuleFor(request => request.TenantId).NotEmpty();
            RuleFor(request => request.OrganizationId).NotEmpty();
            RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        }
    }

    public sealed class SetIdentityTenantStatusValidator : AbstractValidator<SetIdentityTenantStatus>
    {
        public SetIdentityTenantStatusValidator() => RuleFor(request => request.TenantId).NotEmpty();
    }

    public sealed class SetIdentityOrganizationStatusValidator : AbstractValidator<SetIdentityOrganizationStatus>
    {
        public SetIdentityOrganizationStatusValidator()
        {
            RuleFor(request => request.TenantId).NotEmpty();
            RuleFor(request => request.OrganizationId).NotEmpty();
        }
    }

    public sealed class SetIdentityBranchStatusValidator : AbstractValidator<SetIdentityBranchStatus>
    {
        public SetIdentityBranchStatusValidator()
        {
            RuleFor(request => request.TenantId).NotEmpty();
            RuleFor(request => request.OrganizationId).NotEmpty();
            RuleFor(request => request.BranchId).NotEmpty();
        }
    }
}
