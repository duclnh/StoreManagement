using FluentValidation;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Contract.Services.V1.Identity.Validators
{
    public sealed class SetRolePermissionsValidator : AbstractValidator<SetRolePermissionsCommand>
    {
        public SetRolePermissionsValidator()
        {
            RuleFor(request => request.RoleId).NotEmpty();
            RuleFor(request => request.Permissions).NotNull();
            RuleForEach(request => request.Permissions).ChildRules(permission =>
            {
                permission.RuleFor(item => item.FunctionId).NotEmpty().MaximumLength(50);
                permission.RuleFor(item => item.ActionId).NotEmpty().MaximumLength(50);
            });
            RuleFor(request => request.Permissions)
                .Must(permissions => permissions is null
                    || permissions.Select(permission => (permission.FunctionId, permission.ActionId)).Distinct().Count() == permissions.Count)
                .WithMessage("A function/action permission can only be specified once.");
        }
    }
}
