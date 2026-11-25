using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class AssignBranchMemberValidator : AbstractValidator<Command.AssignBranchMember>
    {
        public AssignBranchMemberValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.Data).NotNull();
            RuleFor(x => x.Data.UserId).NotEmpty().When(x => x.Data is not null);
        }
    }
}
