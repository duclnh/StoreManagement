using FluentValidation;

namespace Organization.Contract.Services.V1.Tenants.Validators
{
    public sealed class ListTenantsValidator : AbstractValidator<Query.ListTenants>
    {
        public ListTenantsValidator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(150);
        }
    }
}
