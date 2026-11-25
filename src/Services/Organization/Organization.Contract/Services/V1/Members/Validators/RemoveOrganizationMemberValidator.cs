using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class RemoveOrganizationMemberValidator : AbstractValidator<Command.RemoveOrganizationMember>
    {
        public RemoveOrganizationMemberValidator()
        {
            RuleFor(x => x.OrganizationId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
