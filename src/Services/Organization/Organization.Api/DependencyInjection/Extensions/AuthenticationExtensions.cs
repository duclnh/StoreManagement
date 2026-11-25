using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Organization.Api.Authorization;
using Organization.Domain.Abstractions;
using Organization.Persistence;

namespace Organization.Api.DependencyInjection.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddOrganizationAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<TenantContext>();
            services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
            services.AddScoped<IOrganizationContext>(provider => provider.GetRequiredService<TenantContext>());
            var secret = configuration["JwtOptions:SecretKey"] ?? throw new InvalidOperationException("JwtOptions:SecretKey is required.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = BuildingBlocks.AspNetCore.Authentication.JwtValidation.Create(
                    configuration["JwtOptions:Issuer"] ?? "", configuration["JwtOptions:Audience"] ?? "", secret);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var cancellationToken = context.HttpContext.RequestAborted;
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var tenantId))
                        {
                            context.Fail("Tenant claim is required.");
                            return;
                        }

                        foreach (var claim in new[] { "organization_id", "branch_id" })
                        {
                            var value = principal.FindFirst(claim)?.Value;
                            if (value is not null && !Guid.TryParse(value, out _))
                            {
                                context.Fail("Invalid scope claim.");
                                return;
                            }
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        if (!principal.IsInRole("Administrator") && !await db.Tenants.AsNoTracking().AnyAsync(x => x.Id == tenantId && x.Status, cancellationToken))
                        {
                            context.Fail("Tenant is inactive.");
                            return;
                        }

                        // Identity remains the authority for account/session revocation.
                        var client = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("IdentitySession");
                        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auths/me");
                        request.Headers.Authorization = AuthenticationHeaderValue.Parse(context.Request.Headers.Authorization.ToString());
                        try
                        {
                            using var response = await client.SendAsync(request, cancellationToken);
                            if (!response.IsSuccessStatusCode)
                            {
                                context.Fail("Identity rejected the session.");
                            }
                        }
                        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
                        {
                            context.Fail("Identity session validation is unavailable.");
                        }
                    }
                };
            });
            services.AddAuthorization();
            return services;
        }
    }
}
