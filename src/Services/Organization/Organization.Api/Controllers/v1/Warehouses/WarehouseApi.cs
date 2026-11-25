using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.Warehouses;

namespace Organization.Api.Controllers.v1.Warehouses
{
    public sealed class WarehouseApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-warehouses")
                .MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/branches/{parentId:guid}/warehouses", async (Guid parentId, string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListWarehouses(parentId, search, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/branches/{parentId:guid}/warehouses", async (Guid parentId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Command.CreateWarehouse(parentId, data), cancellationToken);
                return result.IsFailure ? Failure(result) : Results.Created($"/api/v1/warehouses/{result.Value!.Id}", result.Value);
            });
            group.MapGet("/warehouses/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.GetWarehouse(id), cancellationToken)));
            group.MapPut("/warehouses/{id:guid}", async (Guid id, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.UpdateWarehouse(id, data), cancellationToken)));
            group.MapPatch("/warehouses/{id:guid}/status", async (Guid id, StatusRequest data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.SetWarehouseStatus(id, data.Status), cancellationToken)));
        }

        public sealed record StatusRequest(bool Status);
    }
}
