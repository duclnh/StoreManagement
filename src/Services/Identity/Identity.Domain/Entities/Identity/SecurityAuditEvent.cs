namespace Identity.Domain.Entities.Identity
{
    public class SecurityAuditEvent
    {
        protected SecurityAuditEvent()
        {
        }

        public SecurityAuditEvent(
            string eventType,
            string outcome,
            DateTimeOffset occurredAtUtc,
            Guid? actorUserId,
            Guid? subjectUserId,
            Guid? sessionId,
            Guid? tenantId,
            string? ipAddress,
            string? details)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            Outcome = outcome;
            OccurredAtUtc = occurredAtUtc;
            ActorUserId = actorUserId;
            SubjectUserId = subjectUserId;
            SessionId = sessionId;
            TenantId = tenantId;
            IpAddress = ipAddress;
            Details = details;
        }

        public virtual Guid Id { get; protected set; }
        public virtual string EventType { get; protected set; } = string.Empty;
        public virtual string Outcome { get; protected set; } = string.Empty;
        public virtual DateTimeOffset OccurredAtUtc { get; protected set; }
        public virtual Guid? ActorUserId { get; protected set; }
        public virtual Guid? SubjectUserId { get; protected set; }
        public virtual Guid? SessionId { get; protected set; }
        public virtual Guid? TenantId { get; protected set; }
        public virtual string? IpAddress { get; protected set; }
        public virtual string? Details { get; protected set; }
    }

    public static class SecurityAuditEventTypes
    {
        public const string Login = "Authentication.Login";
        public const string Logout = "Authentication.Logout";
        public const string SessionsRevoked = "Authentication.SessionsRevoked";
        public const string PasswordChanged = "Authentication.PasswordChanged";
        public const string PasswordReset = "Authentication.PasswordReset";
        public const string EmailConfirmed = "Authentication.EmailConfirmed";
        public const string TwoFactorEnabled = "Authentication.TwoFactorEnabled";
        public const string TwoFactorDisabled = "Authentication.TwoFactorDisabled";
        public const string UserCreated = "Users.Created";
        public const string UserUpdated = "Users.Updated";
        public const string UserStatusChanged = "Users.StatusChanged";
        public const string UserRolesChanged = "Users.RolesChanged";
        public const string RoleCreated = "Roles.Created";
        public const string RoleUpdated = "Roles.Updated";
        public const string RoleDeleted = "Roles.Deleted";
        public const string RolePermissionsChanged = "Roles.PermissionsChanged";
    }

    public static class SecurityAuditOutcomes
    {
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
    }
}
