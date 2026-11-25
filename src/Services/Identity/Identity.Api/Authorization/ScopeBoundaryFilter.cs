using Identity.Domain.Abstractions;
using Identity.Domain.Repositories;

namespace Identity.Api.Authorization;

/// <summary>Separates permission to act in a scope from access to a particular account.</summary>
public sealed class ScopeBoundaryFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var tenant = http.RequestServices.GetRequiredService<IIdentityTenantContext>();
        if (http.Request.RouteValues.TryGetValue("userId", out var value)
            && Guid.TryParse(value?.ToString(), out var userId))
        {
            var users = http.RequestServices.GetRequiredService<IAppUserRepository>();
            if (tenant.TenantId is not Guid tenantId || !await users.IsInScopeAsync(userId, tenantId, http.RequestAborted))
                return Results.NotFound();
        }

        var path = (http.Request.Path.Value ?? "").TrimEnd('/').ToLowerInvariant();
        var writesGlobalAccount = path.EndsWith("/status") || path.EndsWith("/claims")
            || (http.Request.Method == "PUT" && http.Request.RouteValues.ContainsKey("userId") && !path.EndsWith("/roles"));
        var writesGlobalRole = path.Contains("/roles") && !http.Request.RouteValues.ContainsKey("userId")
            && !path.EndsWith("/permissions");
        if (!HttpMethods.IsGet(http.Request.Method) && (writesGlobalAccount || writesGlobalRole)
            && tenant.RequestedScopeId != tenant.TenantId)
            return Results.Problem(statusCode: 403, detail: "Account-wide and role-definition changes require tenant scope.");

        return await next(context);
    }
}
