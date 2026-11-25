using Identity.Infrastructure.DependencyInjections.Options;
using Identity.Domain.Abstractions;
using Identity.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.DependencyInjection.Extensions
{
    public static class JwtExtensions
    {
        public static void AddJwtAuthenticationApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            ).AddJwtBearer(o =>
            {
                JwtOptions jwtOption = new JwtOptions();
                configuration.GetSection(nameof(JwtOptions)).Bind(jwtOption);

                var key = Encoding.UTF8.GetBytes(jwtOption.SecretKey);
                o.SaveToken = true;

                o.TokenValidationParameters = BuildingBlocks.AspNetCore.Authentication.JwtValidation.Create(
                    jwtOption.Issuer, jwtOption.Audience, jwtOption.SecretKey);

                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var sessionIdValue = context.Principal?.FindFirstValue(ClaimTypes.Sid);
                        if (!Guid.TryParse(userIdValue, out var userId)
                            || !Guid.TryParse(sessionIdValue, out var sessionId))
                        {
                            context.Fail("The token does not identify a valid login session.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var session = await db.LoginSessions.AsNoTracking().FirstOrDefaultAsync(
                            item => item.Id == sessionId, context.HttpContext.RequestAborted);

                        if (session is null
                            || session.UserId != userId
                            || session.IsRevoked
                            || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                        {
                            context.Fail("The login session has been revoked.");
                            return;
                        }
                        var now = DateTimeOffset.UtcNow;
                        if (!await db.Users.AnyAsync(user => user.Id == userId && user.IsActive
                            && (!user.LockoutEnd.HasValue || user.LockoutEnd <= now), context.HttpContext.RequestAborted))
                        {
                            context.Fail("The account is unavailable.");
                            return;
                        }
                        var tenantIdValue = context.Principal?.FindFirstValue(IdentityClaimTypes.TenantId);
                        if (!Guid.TryParse(tenantIdValue, out var tenantId))
                        {
                            context.Fail("A tenant claim is required.");
                            return;
                        }

                        try
                        {
                            var projection = context.HttpContext.RequestServices.GetRequiredService<IOrganizationScopeProjection>();
                            if (!await projection.RefreshAsync(tenantId, context.HttpContext.RequestAborted) && !context.Principal!.IsInRole("Administrator"))
                            {
                                context.Fail("The tenant is unavailable.");
                                return;
                            }
                        }
                        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
                        {
                            context.Fail("Organization directory is unavailable.");
                            return;
                        }
                        if (context.Principal!.IsInRole("Administrator")
                            && Guid.TryParse(context.Request.Headers["X-Tenant-Id"], out var selectedTenantId)
                            && selectedTenantId != tenantId)
                        {
                            var projection = context.HttpContext.RequestServices.GetRequiredService<IOrganizationScopeProjection>();
                            await projection.RefreshAsync(selectedTenantId, context.HttpContext.RequestAborted);
                        }

                        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                        if (session.LastActiveAtUtc < now.AddMinutes(-1) || session.IpAddress != ip)
                            await db.LoginSessions.Where(item => item.Id == sessionId && !item.IsRevoked
                                && (item.LastActiveAtUtc < now.AddMinutes(-1) || item.IpAddress != ip))
                                .ExecuteUpdateAsync(update => update.SetProperty(item => item.LastActiveAtUtc, now)
                                    .SetProperty(item => item.IpAddress, ip), context.HttpContext.RequestAborted);
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if(context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["IS-TOKEN-EXPIRED"] = "true";
                        }
                        return Task.CompletedTask;
                    }
                };
            });


            services.AddAuthorization(options =>
            {
                options.AddPolicy("IdentityAdmin", policy => policy.RequireRole("Administrator"));
            });
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        }
    }
}
