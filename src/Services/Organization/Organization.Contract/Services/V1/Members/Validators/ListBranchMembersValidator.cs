using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class ListBranchMembersValidator : AbstractValidator<Query.ListBranchMembers>
    {
        public ListBranchMembersValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
