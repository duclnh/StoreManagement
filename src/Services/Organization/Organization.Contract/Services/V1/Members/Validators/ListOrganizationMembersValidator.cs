using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class ListOrganizationMembersValidator : AbstractValidator<Query.ListOrganizationMembers>
    {
        public ListOrganizationMembersValidator()
        {
            RuleFor(x => x.OrganizationId).NotEmpty();
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
