using FluentValidation;

namespace Organization.Contract.Services.V1.Organizations.Validators
{
    public sealed class UpdateOrganizationValidator : AbstractValidator<Command.UpdateOrganization>
    {
        public UpdateOrganizationValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
