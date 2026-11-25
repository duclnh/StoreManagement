namespace ApiYarnGateway.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGateway(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks();
            services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
            return services;
        }
    }
}
