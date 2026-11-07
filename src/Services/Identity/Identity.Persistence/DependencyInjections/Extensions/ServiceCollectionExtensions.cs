using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Identity.Persistence.DependencyInjections.Options;
using Identity.Persistence.Interceptors;
using Identity.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Persistence.DependencyInjections.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSqlServerPersistence(this IServiceCollection services)
        {
            services.AddDbContextPool<DbContext, ApplicationDbContext>((provider, builder) =>
            {
                //interceptor
                var outboxInterceptor = provider.GetService<ConvertDomainEventsToOutboxMessagesInterceptor>();
                var auditableInterceptor = provider.GetService<UpdateAuditableEntitiesInterceptor>();

                var configuration = provider.GetRequiredService<IConfiguration>();
                var options = provider.GetRequiredService<IOptionsMonitor<SqlServerRetryOptions>>();

                //#region =========================== SQL Server - Strategy-2 ===========================
                builder
                   .EnableDetailedErrors(true)
                   .EnableSensitiveDataLogging(true)
                   .UseLazyLoadingProxies(true)
                   .UseSqlServer(
                       connectionString: configuration.GetConnectionString("DefaultConnectString"),
                       sqlServerOptionsAction: optionsBuilder
                           => optionsBuilder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
                   .AddInterceptors(outboxInterceptor!, auditableInterceptor!);

                //#endregion  =========================== SQL Server - Strategy-1 ===========================

            });

            services.AddIdentityCore<AppUser>(opt =>
            {
                opt.Lockout.AllowedForNewUsers = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2); //default 5
                opt.Lockout.MaxFailedAccessAttempts = 3;// default 5
            })
                .AddRoles<AppRole>()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = true;
            });
        }

        public static void AddInterceptorPersistence(this IServiceCollection services)
        {
            services.AddSingleton<ConvertDomainEventsToOutboxMessagesInterceptor>();
            services.AddSingleton<UpdateAuditableEntitiesInterceptor>();
        }

        public static void AddRepositoryPersistence(this IServiceCollection services)
        {
            services.AddTransient(typeof(IUnitOfWork), typeof(EFUnitOfWork));
            services.AddTransient(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
            services.AddTransient<IAppUserRepository, AppUserRepository>();
        }

        public static OptionsBuilder<SqlServerRetryOptions> ConfigureSqlServerRetryOptionsPersistence(this IServiceCollection services, IConfigurationSection section)
        {
            return services.AddOptions<SqlServerRetryOptions>()
                            .Bind(section)
                            .ValidateDataAnnotations()
                            .ValidateOnStart();
        }
    }
}

