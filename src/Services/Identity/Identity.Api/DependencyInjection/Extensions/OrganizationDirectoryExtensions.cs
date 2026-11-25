using Identity.Domain.Abstractions;
using Identity.Infrastructure.Organization;
using Identity.Persistence.Repositories;

namespace Identity.Api.DependencyInjection.Extensions
{
    public static class OrganizationDirectoryExtensions
    {
        public static IServiceCollection AddOrganizationDirectory(this IServiceCollection services, IConfiguration configuration)
        {
            var address = new Uri(configuration["Services:Organization:BaseUrl"]
                ?? throw new InvalidOperationException("Services:Organization:BaseUrl is required after ownership cutover."));
            if (address.Scheme != "https" && !address.IsLoopback)
            {
                throw new InvalidOperationException("Organization requires HTTPS outside loopback.");
            }

            var key = configuration["ServiceAuthentication:Key"];
            if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            {
                throw new InvalidOperationException("ServiceAuthentication:Key must contain at least 32 characters.");
            }

            services.AddHttpClient<IOrganizationScopeDirectory, OrganizationScopeDirectory>(client =>
            {
                client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("X-Service-Key", key);
                client.Timeout = TimeSpan.FromSeconds(5);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            services.AddScoped<IOrganizationScopeProjection, OrganizationScopeProjection>();
            return services;
        }
    }
}
