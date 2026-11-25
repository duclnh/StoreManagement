using FluentValidation;

namespace Organization.Contract.Services.V1.Branches.Validators
{
    public sealed class ListBranchesValidator : AbstractValidator<Query.ListBranches>
    {
        public ListBranchesValidator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(150);
        }
    }
}
