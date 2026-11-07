using FluentValidation;
using AccountCommand = Identity.Contract.Services.V1.Identity.Command;
using AccountQuery = Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Contract.Services.V1.Identity.Validators
{
    public sealed class RegisterValidator : AbstractValidator<AccountCommand.Register>
    {
        public RegisterValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
            RuleFor(request => request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.LastName).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class CreateManagedUserValidator : AbstractValidator<AccountCommand.CreateManagedUser>
    {
        public CreateManagedUserValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
            RuleFor(request => request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.LastName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.Roles).NotNull();
        }
    }

    public sealed class ChangePasswordValidator : AbstractValidator<AccountCommand.ChangePassword>
    {
        public ChangePasswordValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.CurrentPassword).NotEmpty();
            RuleFor(request => request.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .NotEqual(request => request.CurrentPassword)
                .WithMessage("New password must differ from the current password.");
        }
    }

    public sealed class CreateAuthenticatorSetupValidator : AbstractValidator<AccountCommand.CreateAuthenticatorSetup>
    {
        public CreateAuthenticatorSetupValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
        }
    }

    public sealed class EnableTwoFactorValidator : AbstractValidator<AccountCommand.EnableTwoFactor>
    {
        public EnableTwoFactorValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.Code).Must(AccountCommandValidators.IsValidAuthenticatorCode)
                .WithMessage("Authenticator code must contain six digits.");
        }
    }

    public sealed class DisableTwoFactorValidator : AbstractValidator<AccountCommand.DisableTwoFactor>
    {
        public DisableTwoFactorValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.Code).Must(AccountCommandValidators.IsValidAuthenticatorCode)
                .WithMessage("Authenticator code must contain six digits.");
        }
    }

    public sealed class RequestEmailConfirmationValidator : AbstractValidator<AccountCommand.RequestEmailConfirmation>
    {
        public RequestEmailConfirmationValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
        }
    }

    public sealed class ConfirmEmailValidator : AbstractValidator<AccountCommand.ConfirmEmail>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
            RuleFor(request => request.Token).NotEmpty();
        }
    }

    public sealed class RequestPasswordResetValidator : AbstractValidator<AccountCommand.RequestPasswordReset>
    {
        public RequestPasswordResetValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
        }
    }

    public sealed class ResetPasswordValidator : AbstractValidator<AccountCommand.ResetPassword>
    {
        public ResetPasswordValidator()
        {
            RuleFor(request => request.Email).NotEmpty().EmailAddress();
            RuleFor(request => request.Token).NotEmpty();
            RuleFor(request => request.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }

    public sealed class UpdateUserProfileValidator : AbstractValidator<AccountCommand.UpdateUserProfile>
    {
        public UpdateUserProfileValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.ActorId).NotEmpty();
            RuleFor(request => request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.LastName).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class SetUserActiveValidator : AbstractValidator<AccountCommand.SetUserActive>
    {
        public SetUserActiveValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.ActorId).NotEmpty();
        }
    }

    public sealed class AssignUserRolesValidator : AbstractValidator<AccountCommand.AssignUserRoles>
    {
        public AssignUserRolesValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.ActorId).NotEmpty();
            RuleFor(request => request.Roles).NotNull().Must(roles => roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count)
                .WithMessage("Role names must be unique.");
            RuleForEach(request => request.Roles).NotEmpty().MaximumLength(50);
        }
    }

    public sealed class CreateRoleValidator : AbstractValidator<AccountCommand.CreateRole>
    {
        public CreateRoleValidator()
        {
            RuleFor(request => request.Name).NotEmpty().MaximumLength(50);
            RuleFor(request => request.RoleCode).NotEmpty().MaximumLength(50).Matches("^[A-Z0-9_-]+$");
            RuleFor(request => request.Description).NotEmpty().MaximumLength(250);
        }
    }

    public sealed class UpdateRoleValidator : AbstractValidator<AccountCommand.UpdateRole>
    {
        public UpdateRoleValidator()
        {
            RuleFor(request => request.RoleId).NotEmpty();
            RuleFor(request => request.Name).NotEmpty().MaximumLength(50);
            RuleFor(request => request.RoleCode).NotEmpty().MaximumLength(50).Matches("^[A-Z0-9_-]+$");
            RuleFor(request => request.Description).NotEmpty().MaximumLength(250);
        }
    }

    public sealed class DeleteRoleValidator : AbstractValidator<AccountCommand.DeleteRole>
    {
        public DeleteRoleValidator()
        {
            RuleFor(request => request.RoleId).NotEmpty();
        }
    }

    public sealed class GetUsersValidator : AbstractValidator<AccountQuery.GetUsers>
    {
        public GetUsersValidator()
        {
            RuleFor(request => request.Search).MaximumLength(100);
            RuleFor(request => request.Page).GreaterThan(0);
            RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class GetUserValidator : AbstractValidator<AccountQuery.GetUser>
    {
        public GetUserValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
        }
    }

    public sealed class RefreshTokenValidator : AbstractValidator<AccountQuery.RefreshToken>
    {
        public RefreshTokenValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.SessionId).NotEmpty();
            RuleFor(request => request.Token).NotEmpty();
        }
    }

    public sealed class RevokeTokenValidator : AbstractValidator<AccountQuery.RevokeToken>
    {
        public RevokeTokenValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.SessionId).NotEmpty();
        }
    }

    public sealed class ListSessionsValidator : AbstractValidator<AccountQuery.ListSessions>
    {
        public ListSessionsValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.CurrentSessionId).NotEmpty();
        }
    }

    public sealed class RevokeOtherSessionsValidator : AbstractValidator<AccountQuery.RevokeOtherSessions>
    {
        public RevokeOtherSessionsValidator()
        {
            RuleFor(request => request.UserId).NotEmpty();
            RuleFor(request => request.CurrentSessionId).NotEmpty();
        }
    }

    internal static class AccountCommandValidators
    {
        internal static bool IsValidAuthenticatorCode(string? code)
        {
            var normalizedCode = code?.Replace(" ", string.Empty).Replace("-", string.Empty);
            return normalizedCode is { Length: 6 } && normalizedCode.All(char.IsDigit);
        }
    }
}