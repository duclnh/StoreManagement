using Identity.Contract.Abstraction.Message;
using MediatR;
using static Identity.Contract.Services.V1.Identity.Response;


namespace Identity.Contract.Services.V1.Identity
{
    public static class Query
    {
        public record Login(
            string Email,
            string Password,
            string? UserAgent = null,
            string? IpAddress = null,
            string? TwoFactorCode = null) : IQuery<Authenticated>;
        public record RefreshToken(Guid UserId, Guid SessionId, string Token) : IQuery<Authenticated>;
        public record RevokeToken(Guid UserId, Guid SessionId) : IQuery<Unit>;
        public record ListSessions(Guid UserId, Guid CurrentSessionId) : IQuery<List<LoginSession>>;
        public record RevokeOtherSessions(Guid UserId, Guid CurrentSessionId) : IQuery<int>;
        public record GetUsers(string? Search, int Page = 1, int PageSize = 25) : IQuery<Response.UserPage>;
        public record GetUser(Guid UserId) : IQuery<Response.AdminUser>;
        public record GetUserRoles(Guid UserId, Guid TenantId, Guid ScopeId) : IQuery<List<string>>;
        public record GetRoles() : IQuery<List<Response.AdminRole>>;
        public record GetIdentityScopes(Guid TenantId) : IQuery<List<Response.IdentityScope>>;
        public record GetRolePermissions(Guid RoleId, Guid TenantId, Guid ScopeId) : IQuery<List<Response.RolePermission>>;
        public record GetSecurityAuditEvents(
            DateTimeOffset? FromUtc,
            DateTimeOffset? ToUtc,
            string? EventType,
            string? Outcome,
            Guid? UserId,
            int Page = 1,
            int PageSize = 50) : IQuery<Response.SecurityAuditPage>;
    }
}
