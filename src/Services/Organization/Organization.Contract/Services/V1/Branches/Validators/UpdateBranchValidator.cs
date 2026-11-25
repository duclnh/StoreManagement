using FluentValidation;

namespace Organization.Contract.Services.V1.Branches.Validators
{
    public sealed class UpdateBranchValidator : AbstractValidator<Command.UpdateBranch>
    {
        public UpdateBranchValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
