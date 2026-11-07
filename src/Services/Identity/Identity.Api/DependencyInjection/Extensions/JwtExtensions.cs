using Identity.Infrastructure.DependencyInjections.Options;
using Identity.Domain.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Identity.Api.DependencyInjection.Extensions
{
    public static class JwtExtensions
    {
        public static void AddJwtAuthenticationApplication(this IServiceCollection services, IConfiguration configuration)
        {
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

                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

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

                        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
                        var session = await unitOfWork.LoginSessionRepository.FindByIdAsync(
                            sessionId,
                            context.HttpContext.RequestAborted);

                        if (session is null
                            || session.UserId != userId
                            || session.IsRevoked
                            || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                            context.Fail("The login session has been revoked.");
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
        }
    }
}

