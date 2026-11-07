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

        public sealed record UserPage(
            IReadOnlyList<AdminUser> Items,
            int Page,
            int PageSize,
            int TotalCount);

        public sealed record AdminRole(
            Guid Id,
            string Name,
            string RoleCode,
            string Description,
            int UserCount);

        public sealed record IdentityScope(Guid Id, string Name, int ScopeType, Guid? ParentId);

        public sealed record RolePermission(
            string FunctionId,
            string FunctionName,
            string ActionId,
            string ActionName,
            bool IsGranted);

        public sealed record SecurityAuditEvent(
            Guid Id,
            string EventType,
            string Outcome,
            DateTimeOffset OccurredAtUtc,
            Guid? ActorUserId,
            Guid? SubjectUserId,
            Guid? SessionId,
            string? IpAddress,
            string? Details);

        public sealed record SecurityAuditPage(
            IReadOnlyList<SecurityAuditEvent> Items,
            int Page,
            int PageSize,
            int TotalCount);
    }
}
