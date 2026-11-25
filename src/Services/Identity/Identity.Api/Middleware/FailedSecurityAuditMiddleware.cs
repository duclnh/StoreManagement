using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;

namespace Identity.Api.Middleware;

/// <summary>Records unsuccessful security mutations without logging request bodies or credentials.</summary>
public sealed class FailedSecurityAuditMiddleware(ILogger<FailedSecurityAuditMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);
        if (context.Response.StatusCode < 400 || HttpMethods.IsGet(context.Request.Method)) return;
        var path = context.Request.Path.Value ?? "";
        var eventType = path switch
        {
            _ when path.EndsWith("/password/change") => SecurityAuditEventTypes.PasswordChanged,
            _ when path.EndsWith("/password/reset") => SecurityAuditEventTypes.PasswordReset,
            _ when path.EndsWith("/email/confirm") => SecurityAuditEventTypes.EmailConfirmed,
            _ when path.EndsWith("/2fa/enable") => SecurityAuditEventTypes.TwoFactorEnabled,
            _ when path.EndsWith("/2fa/disable") => SecurityAuditEventTypes.TwoFactorDisabled,
            _ when path.Contains("/admin/roles") && path.EndsWith("/permissions") => SecurityAuditEventTypes.RolePermissionsChanged,
            _ when path.Contains("/admin/users/") && path.EndsWith("/roles") => SecurityAuditEventTypes.UserRolesChanged,
            _ when path.Contains("/admin/users/") && path.EndsWith("/status") => SecurityAuditEventTypes.UserStatusChanged,
            _ when path.Contains("/admin/users/") => SecurityAuditEventTypes.UserUpdated,
            _ when path.Contains("/admin/roles") => SecurityAuditEventTypes.RoleUpdated,
            _ when path.EndsWith("/logout") || path.EndsWith("/revoke") => SecurityAuditEventTypes.Logout,
            _ => null
        };
        if (eventType is null) return;
        try
        {
            // A failed command may leave tracked mutations: audit in a fresh unit of work.
            await using var scope = context.RequestServices.CreateAsyncScope();
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            Guid.TryParse(context.Request.RouteValues["userId"]?.ToString(), out var subject);
            unit.SecurityAuditRepository.Record(eventType, SecurityAuditOutcomes.Failed,
                subject == Guid.Empty ? null : subject, $"HTTP:{context.Response.StatusCode}");
            await unit.SaveChangeAsync(CancellationToken.None);
        }
        catch (Exception exception) { logger.LogError(exception, "Failed to persist a security audit outcome"); }
    }
}
