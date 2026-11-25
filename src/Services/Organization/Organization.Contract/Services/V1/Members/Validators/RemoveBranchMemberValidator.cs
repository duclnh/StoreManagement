using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class RemoveBranchMemberValidator : AbstractValidator<Command.RemoveBranchMember>
    {
        public RemoveBranchMemberValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
