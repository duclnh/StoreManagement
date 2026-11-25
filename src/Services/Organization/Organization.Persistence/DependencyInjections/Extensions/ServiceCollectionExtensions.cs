using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organization.Domain.Abstractions;
using Organization.Domain.Repositories;
using Organization.Persistence.Repositories;

namespace Organization.Persistence.DependencyInjections.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrganizationPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
                configuration.GetConnectionString("Organization") ?? throw new InvalidOperationException("ConnectionStrings:Organization is required."),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "organization")));
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<IScopeDirectoryRepository, ScopeDirectory>();
            services.AddScoped<IUnitOfWork, EFUnitOfWork>();
            services.AddScoped<ICommandTransaction, CommandTransaction>();
            return services;
        }
    }
}
