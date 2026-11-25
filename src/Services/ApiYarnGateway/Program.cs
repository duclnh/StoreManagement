using ApiYarnGateway.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGateway(builder.Configuration);

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();

public partial class Program
{
}
