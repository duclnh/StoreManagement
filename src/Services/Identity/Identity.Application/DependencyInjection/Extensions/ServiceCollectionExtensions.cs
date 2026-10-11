using Identity.Application.Behaviors;
using Identity.Application.Mapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediatorApplication(this IServiceCollection services)
            => services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformancePipelineBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>))
            .AddValidatorsFromAssembly(Contract.AssemblyReference.Assembly, includeInternalTypes: true);

        public static IServiceCollection AddAutoMapperApplication(this IServiceCollection services)
            => services.AddAutoMapper(typeof(ServiceProfile));
    }
}


