using Carter;
using DistributedSystem.Api.DependencyInjection.Extensions;
using DistributedSystem.Api.Middleware;
using DistributedSystem.Application.DependencyInjection.Extensions;
using DistributedSystem.Infrastructure.DependencyInjections.Extensions;
using DistributedSystem.Persistence.DependencyInjections.Extensions;
using DistributedSystem.Persistence.DependencyInjections.Options;
using HealthChecks.UI.Client;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging
    .ClearProviders()
    .AddSerilog();

builder.Host.UseSerilog();

builder.Services.AddCarter();


//=> define api controller in Presentation
//builder.Services.AddControllers()
//    .AddApplicationPart(DistributedSystem.Presentation.AssemblyReference.Assembly);

builder.Services
    .AddSwaggerGenNewtonsoftSupport()
    .AddFluentValidationRulesToSwagger()
    .AddEndpointsApiExplorer()
    .AddSwaggerApi();

builder.Services
    .AddApiVersioning(options => options.ReportApiVersions = true)
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddJwtAuthenticationApplication(builder.Configuration);


builder.Services.AddMediatorApplication();
builder.Services.AddAutoMapperApplication();

builder.Services.AddQuartzInfrastructure();
builder.Services.AddServiceInfrastructure();
builder.Services.AddMediatRInfrastructure();
builder.Services.AddRedisInfrastructure(builder.Configuration);
builder.Services.AddMassTransitRabbitMQInfrastructure(builder.Configuration);
builder.Services.AddHealthChecksInfrastructure(builder.Configuration);
builder.Services.ConfigurationServicesInfrastructure(builder.Configuration);


builder.Services.AddInterceptorPersistence();
builder.Services.AddSqlServerPersistence();
builder.Services.ConfigureSqlServerRetryOptionsPersistence(builder.Configuration.GetSection(nameof(SqlServerRetryOptions)));
builder.Services.AddRepositoryPersistence();


builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapCarter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwaggerApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

//app.MapControllers();
try
{
    await app.RunAsync();
    Log.Information("Stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during booostrapping");
    await app.StopAsync();
}
finally
{

    Log.CloseAndFlush();
    await app.DisposeAsync();
}

public partial class Program { }
