using FluentValidation;

namespace Organization.Contract.Services.V1.Organizations.Validators
{
    public sealed class ListOrganizationsValidator : AbstractValidator<Query.ListOrganizations>
    {
        public ListOrganizationsValidator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(150);
        }
    }
}
