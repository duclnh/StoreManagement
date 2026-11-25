using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.Tenants;

namespace Organization.Api.Controllers.v1.Tenants
{
    public sealed class TenantApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-tenants")
                .MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/tenants", async (string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListTenants(null, search, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/tenants", async (Command.Data data, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Command.CreateTenant(data), cancellationToken);
                return result.IsFailure ? Failure(result) : Results.Created($"/api/v1/tenants/{result.Value!.Id}", result.Value);
            });
            group.MapGet("/tenants/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.GetTenant(id), cancellationToken)));
            group.MapPut("/tenants/{id:guid}", async (Guid id, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.UpdateTenant(id, data), cancellationToken)));
            group.MapPatch("/tenants/{id:guid}/status", async (Guid id, StatusRequest data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.SetTenantStatus(id, data.Status), cancellationToken)));
        }

        public sealed record StatusRequest(bool Status);
    }
}
