using FluentValidation;

namespace Organization.Contract.Services.V1.Members.Validators
{
    public sealed class AddOrganizationMemberValidator : AbstractValidator<Command.AddOrganizationMember>
    {
        public AddOrganizationMemberValidator()
        {
            RuleFor(x => x.OrganizationId).NotEmpty();
            RuleFor(x => x.Data).NotNull();
            RuleFor(x => x.Data.UserId).NotEmpty().When(x => x.Data is not null);
        }
    }
}
