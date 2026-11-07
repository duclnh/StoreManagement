using Identity.Contract.Abstraction.Message;
using static Identity.Contract.Services.V1.Identity.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Identity.Contract.Services.V1.Identity
{
    public static class Command
    {
        public record Register(string Email, string Password, string FirstName, string LastName) : ICommand<Guid>;
        public record RevokeSession(Guid SessionId);
        public record ChangePassword(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<bool>;
        public record CreateAuthenticatorSetup(Guid UserId) : ICommand<AuthenticatorSetup>;
        public record EnableTwoFactor(Guid UserId, string Code) : ICommand<bool>;
        public record DisableTwoFactor(Guid UserId, string Code) : ICommand<bool>;
        public record RequestEmailConfirmation(string Email) : ICommand<bool>;
        public record ConfirmEmail(string Email, string Token) : ICommand<bool>;
        public record RequestPasswordReset(string Email) : ICommand<bool>;
        public record ResetPassword(string Email, string Token, string NewPassword) : ICommand<bool>;
        public record UpdateUserProfile(Guid UserId, Guid ActorId, string FirstName, string LastName) : ICommand<bool>;
        public record SetUserActive(Guid UserId, Guid ActorId, bool IsActive) : ICommand<bool>;
        public record AssignUserRoles(Guid UserId, Guid ActorId, List<string> Roles) : ICommand<bool>;
        public record CreateManagedUser(string Email, string Password, string FirstName, string LastName, List<string> Roles) : ICommand<Guid>;
        public record CreateRole(string Name, string RoleCode, string Description) : ICommand<Guid>;
        public record UpdateRole(Guid RoleId, string Name, string RoleCode, string Description) : ICommand<bool>;
        public record DeleteRole(Guid RoleId) : ICommand<bool>;
        public sealed record UpdateUserRequest(string FirstName, string LastName);
        public sealed record SetUserActiveRequest(bool IsActive);
        public sealed record AssignUserRolesRequest(List<string> Roles);
        public sealed record CreateManagedUserRequest(string Email, string Password, string FirstName, string LastName, List<string> Roles);
        public sealed record UpdateRoleRequest(string Name, string RoleCode, string Description);
        public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
        public record TwoFactorCode(string Code);
    }
}

