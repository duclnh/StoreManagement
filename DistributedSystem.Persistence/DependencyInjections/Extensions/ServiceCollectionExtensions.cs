using DistributedSystem.Domain.Abstractions;
using DistributedSystem.Domain.Entities.Identity;
using DistributedSystem.Domain.Repositories;
using DistributedSystem.Persistence.DependencyInjections.Options;
using DistributedSystem.Persistence.Interceptors;
using DistributedSystem.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Persistence.DependencyInjections.Extensions
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

                #region =========================== SQL Server - Strategy-1 ===========================
                builder
                    .EnableDetailedErrors(true)
                    .EnableSensitiveDataLogging(true)
                    .UseLazyLoadingProxies(true)
                    .UseSqlServer(
                        connectionString: configuration.GetConnectionString("DefaultConnectString"),
                        sqlServerOptionsAction: optionsBuider 
                            => optionsBuider.ExecutionStrategy(
                                dependencies => new SqlServerRetryingExecutionStrategy(
                                    dependencies: dependencies,
                                    maxRetryCount: options.CurrentValue.MaxRetry,
                                    maxRetryDelay: options.CurrentValue.MaxRetryDelay,
                                    errorNumbersToAdd: options.CurrentValue.ErrorNumbersToAdd))
                               .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
                    .AddInterceptors(outboxInterceptor, auditableInterceptor);

                #endregion  =========================== SQL Server - Strategy-1 ===========================

                //#region =========================== SQL Server - Strategy-2 ===========================
                //builder
                //    .EnableDetailedErrors(true)
                //    .EnableSensitiveDataLogging(true)
                //    .UseLazyLoadingProxies(true)
                //    .UseSqlServer(
                //        connectionString: configuration.GetConnectionString("DefaultConnectString"),
                //        sqlServerOptionsAction: optionsBuider
                //            => optionsBuider.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
                //    .AddInterceptors(outboxInterceptor, auditableInterceptor);

                //#endregion  =========================== SQL Server - Strategy-1 ===========================

            });

            services.AddIdentityCore<AppUser>(opt =>
            {
                opt.Lockout.AllowedForNewUsers = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2); //default 5
                opt.Lockout.MaxFailedAccessAttempts = 3;// default 5
            })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
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
            
            services.AddTransient(typeof(IUnitOfWorkDbContext<>), typeof(EFUnitOfWorkDbContext<>));
            services.AddTransient(typeof(IRepositoryBaseDbContext<,,>), typeof(RepositoryBaseDbContext<,,>));

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
