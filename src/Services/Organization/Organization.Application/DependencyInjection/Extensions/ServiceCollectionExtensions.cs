using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Organization.Application.Abstractions;
using Organization.Application.Authorization;
using Organization.Application.Behaviors;

namespace Organization.Application.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrganizationApplication(this IServiceCollection services)
        {
            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(AssemblyReference.Assembly);
                options.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
                options.AddOpenBehavior(typeof(PerformancePipelineBehavior<,>));
                options.AddOpenBehavior(typeof(TransactionPipelineBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Contract.AssemblyReference.Assembly);
            services.AddScoped<IScopeAccess, ScopeAccess>();
            return services;
        }
    }
}
