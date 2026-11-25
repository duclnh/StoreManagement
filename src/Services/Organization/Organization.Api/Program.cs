using Carter;
using Organization.Api.DependencyInjection.Extensions;
using Organization.Api.Middleware;
using Organization.Application.DependencyInjection.Extensions;
using Organization.Infrastructure.DependencyInjections.Extensions;
using Organization.Persistence.DependencyInjections.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApiVersioning(options => options.ReportApiVersions = true);
builder.Services.AddOrganizationApplication();
builder.Services.AddOrganizationPersistence(builder.Configuration);
builder.Services.AddOrganizationInfrastructure(builder.Configuration);
builder.Services.AddOrganizationAuthentication(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<Organization.Api.Health.DatabaseHealthCheck>("organization-database");

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();
app.MapHealthChecks("/health");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program
{
}
