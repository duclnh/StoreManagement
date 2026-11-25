using FluentValidation;

namespace Organization.Contract.Services.V1.Tenants.Validators
{
    public sealed class UpdateTenantValidator : AbstractValidator<Command.UpdateTenant>
    {
        public UpdateTenantValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
