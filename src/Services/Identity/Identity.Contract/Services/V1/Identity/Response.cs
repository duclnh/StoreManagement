namespace Identity.Contract.Services.V1.Identity
{
    public static class Response
    {
        public class Authenticated
        {
            public Guid UserId { get; set; }
            public Guid SessionId { get; set; }
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime RefreshTokenExpiryTime { get; set; }
        };

        public class LoginSession
        {
            public Guid SessionId { get; set; }
            public string? UserAgent { get; set; }
            public string DeviceCategory { get; set; } = "Computer";
            public string? IpAddress { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime LastActiveAtUtc { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public bool IsCurrent { get; set; }
        }

        public sealed record AuthenticatorSetup(string SharedKey, string AuthenticatorUri);

        public sealed record AdminUser(
            Guid Id,
            string Email,
            string FirstName,
            string LastName,
            string FullName,
            bool IsActive,
            bool EmailConfirmed,
            bool TwoFactorEnabled,
            IReadOnlyList<string> Roles);

        public sealed record CurrentUserProfile(
            Guid Id,
            Guid TenantId,
            bool IsAdministrator,
            bool TwoFactorEnabled);

        public sealed record IdentityAdminAccess(
            Guid TenantId,
            Guid ScopeId,
            bool IsAdministrator,
            bool CanViewUsers,
            bool CanCreateUsers,
            bool CanUpdateUsers,
            bool CanAssignUsers,
            bool CanViewRoles,
            bool CanCreateRoles,
            bool CanUpdateRoles,
            bool CanDeleteRoles,
            bool CanManageRolePermissions,
            bool CanViewAudit,
            bool CanViewSessions,
            bool CanRevokeSessions);

        public sealed record AdminRole(
            Guid Id,
            string Name,
            string RoleCode,
            string Description,
            int UserCount);

        public sealed record IdentityScope(Guid Id, string Name, int ScopeType, Guid? ParentId);
        public sealed record IdentityTenant(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAtUtc);
        public sealed record IdentityAdminScope(Guid Id, string Name, int ScopeType, Guid? ParentId, bool IsActive);

        public sealed record RolePermission(
            string FunctionId,
            string FunctionName,
            string ActionId,
            string ActionName,
            bool IsGranted);

        public sealed record IdentityClaim(string Type, string Value);

        public sealed record SecurityAuditEvent(
            Guid Id,
            string EventType,
            string Outcome,
            DateTimeOffset OccurredAtUtc,
            Guid? ActorUserId,
            string? ActorUserName,
            Guid? SubjectUserId,
            string? SubjectUserName,
            Guid? SessionId,
            string? IpAddress,
            string? Details);

    }
}
