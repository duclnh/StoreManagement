using DistributedSystem.Application.Abstractions;
using DistributedSystem.Constract.JsonConverters;
using DistributedSystem.Infrastructure.Authentication;
using DistributedSystem.Infrastructure.BackgroundJobs;
using DistributedSystem.Infrastructure.Caching;
using DistributedSystem.Infrastructure.Consumers.Abstract.Repositories;
using DistributedSystem.Infrastructure.Consumers.Repositories;
using DistributedSystem.Infrastructure.DependencyInjections.Options;
using DistributedSystem.Infrastructure.PipeObservers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Infrastructure.DependencyInjections.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigurationServicesInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbOptions>(configuration.GetSection(nameof(MongoDbOptions)));
            
            services.AddSingleton<IMongoDbOptions>(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value
            );

            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            
        }

        public static void AddServiceInfrastructure(this IServiceCollection services)
        {
            services.AddTransient<IJwtTokenService, JwtTokenService>()
                .AddTransient<ICacheService, CacheService>();

            //services.Configure<JwtOption>(configuration.GetSection("JwtOption")); if you use IOptions<JwtOption>
        }

        public static void AddRedisInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(
                redisOptions =>
                {
                    var connectString = configuration.GetConnectionString("Redis");
                    redisOptions.Configuration = connectString;
                });
        }


        public static IServiceCollection AddMassTransitRabbitMQInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var massTransitOptions = new MassTransitOptions();

            configuration.GetSection(nameof(MassTransitOptions)).Bind(massTransitOptions);

            var messageBusOptions = new MessageBusOptions();

            configuration.GetSection(nameof(MessageBusOptions)).Bind(messageBusOptions);


            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumers(Assembly.GetExecutingAssembly());//add consumer to masstransitMQ

                cfg.SetKebabCaseEndpointNameFormatter(); //ProductCreate => product-create

                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(massTransitOptions.Host, massTransitOptions.VHost, h =>
                    {
                        h.Username(massTransitOptions.UserName);
                        h.Password(massTransitOptions.Password);
                    });

                    bus.UseMessageRetry(retry =>
                    retry.Incremental(
                        retryLimit: messageBusOptions.RetryLimit,
                        intervalIncrement: messageBusOptions.IntervalIncrement,
                        initialInterval: messageBusOptions.InitialInterval));

                    bus.ConfigureNewtonsoftJsonSerializer(settings =>
                    {
                        settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Objects));
                        settings.Converters.Add(new DateOnlyJsonConverter());
                        settings.Converters.Add(new ExpirationDateOnlyJsonConverter());
                        return settings;
                    });

                    bus.ConfigureNewtonsoftJsonDeserializer(settings =>
                    {
                        settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Objects));
                        settings.Converters.Add(new DateOnlyJsonConverter());
                        settings.Converters.Add(new ExpirationDateOnlyJsonConverter());
                        return settings;
                    });

                    bus.ConnectReceiveObserver(new LoggingReceiveObserver());
                    bus.ConnectConsumeObserver(new LoggingConsumeObserver());
                    bus.ConnectPublishObserver(new LoggingPublishObserver());
                    bus.ConnectSendObserver(new LoggingSendObserver());

                    //SmsNotification => sms-notification
                    bus.MessageTopology.SetEntityNameFormatter(new KebabCaseEntityNameFormatter());

                    bus.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        public static void AddQuartzInfrastructure(this IServiceCollection services)
        {
            services.AddQuartz(
            cfg =>
            {
                var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

                cfg.AddJob<ProcessOutboxMessagesJob>(jobKey)
                   .AddTrigger(trigger =>
                        trigger.ForJob(jobKey)
                               .WithSimpleSchedule(schedule =>
                                    schedule.WithInterval(TimeSpan.FromMicroseconds(100))
                                            .RepeatForever())

                   );
                cfg.UseMicrosoftDependencyInjectionJobFactory();
            });

            services.AddQuartzHostedService();
        }

        public static IServiceCollection AddHealthChecksInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var healthChecks = services.AddHealthChecks();

            // SQL Server
            var sqlConnection = configuration.GetConnectionString("DefaultConnectString");
            if (!string.IsNullOrWhiteSpace(sqlConnection))
            {
                healthChecks.AddSqlServer(
                    connectionString: sqlConnection,
                    name: "sql",
                    healthQuery: "SELECT 1;", // optional custom query
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql" });
            }

            // Redis
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                healthChecks.AddRedis(
                    redisConnectionString: redisConnection,
                    name: "redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "cache", "redis" });
            }

            healthChecks.AddRabbitMQ();

            //Mongodb
            //var mongodbConnection = configuration["MongoDbOptions:DefaultConnectString"] ?? "";
            //var mongodbConnection = configuration.GetSection("MongoDbOptions:DefaultConnectString").Value;
            healthChecks.AddMongoDb();

            return services;
        }

        public static void AddMediatRInfrastructure(this IServiceCollection services)
            => services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly));
    }
}
