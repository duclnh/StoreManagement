using FluentValidation;

namespace Organization.Contract.Services.V1.Organizations.Validators
{
    public sealed class CreateOrganizationValidator : AbstractValidator<Command.CreateOrganization>
    {
        public CreateOrganizationValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
