using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organization.Application.Abstractions;
using Organization.Infrastructure.Identity;

namespace Organization.Infrastructure.DependencyInjections.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrganizationInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var address = new Uri(configuration["Services:Identity:BaseUrl"] ?? throw new InvalidOperationException("Services:Identity:BaseUrl is required."));
            if (address.Scheme != "https" && !address.IsLoopback)
            {
                throw new InvalidOperationException("Identity requires HTTPS outside loopback.");
            }

            var key = configuration["ServiceAuthentication:Key"];
            if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            {
                throw new InvalidOperationException("ServiceAuthentication:Key must contain at least 32 characters.");
            }

            services.AddHttpClient<IIdentityDirectory, IdentityDirectory>(client =>
            {
                client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("X-Service-Key", key);
                client.Timeout = TimeSpan.FromSeconds(5);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            services.AddHttpClient("IdentitySession", client =>
            {
                client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(5);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            return services;
        }
    }
}
