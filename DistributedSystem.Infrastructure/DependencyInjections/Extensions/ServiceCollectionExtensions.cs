using DistributedSystem.Application.Abstractions;
using DistributedSystem.Constract.JsonConverters;
using DistributedSystem.Infrastructure.Authentication;
using DistributedSystem.Infrastructure.Caching;
using DistributedSystem.Infrastructure.DependencyInjections.Options;
using DistributedSystem.Infrastructure.PipeObservers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
            var masstransitOptions = new MasstransitOptions();

            configuration.GetSection(nameof(MasstransitOptions)).Bind(masstransitOptions);

            var messageBusOptions = new MessageBusOptions();

            configuration.GetSection(nameof(MessageBusOptions)).Bind(messageBusOptions);


            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumers(Assembly.GetExecutingAssembly());//add consumer to masstransitMQ

                cfg.SetKebabCaseEndpointNameFormatter(); //ProductCreate => product-create

                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host($"{masstransitOptions.Host}:${masstransitOptions.Port}/{masstransitOptions.VHost}", h =>
                    {
                        h.Username(masstransitOptions.UserName) ;
                        h.Password(masstransitOptions.Password);
                    });

                    bus.UseMessageRetry(retry =>
                    retry.Incremental(
                        retryLimit: messageBusOptions.RetryLimit,
                        intervalIncrement: messageBusOptions.IntervalIncrement,
                        initialInterval: messageBusOptions.InitialInterval));

                    bus.ConfigureNewtonsoftJsonSerializer(settings =>
                    {
                        settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Object));
                        settings.Converters.Add(new DateOnlyJsonConverter());
                        settings.Converters.Add(new ExpirationDateOnlyJsonConverter());
                        return settings;
                    });

                    bus.ConfigureNewtonsoftJsonDeserializer(settings =>
                    {
                        settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Object));
                        settings.Converters.Add(new DateOnlyJsonConverter());
                        settings.Converters.Add(new ExpirationDateOnlyJsonConverter());
                        return settings;
                    }   );

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

    }
}
