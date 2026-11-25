using FluentValidation;

namespace Organization.Contract.Services.V1.Branches.Validators
{
    public sealed class CreateBranchValidator : AbstractValidator<Command.CreateBranch>
    {
        public CreateBranchValidator()
        {
            RuleFor(x => x.OrganizationId).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
