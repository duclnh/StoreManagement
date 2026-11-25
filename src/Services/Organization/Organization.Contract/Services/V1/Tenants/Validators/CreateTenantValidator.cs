using FluentValidation;

namespace Organization.Contract.Services.V1.Tenants.Validators
{
    public sealed class CreateTenantValidator : AbstractValidator<Command.CreateTenant>
    {
        public CreateTenantValidator()
        {

            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
