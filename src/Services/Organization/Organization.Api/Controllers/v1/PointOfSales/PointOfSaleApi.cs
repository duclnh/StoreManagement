using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.PointOfSales;

namespace Organization.Api.Controllers.v1.PointOfSales
{
    public sealed class PointOfSaleApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-pos")
                .MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/branches/{parentId:guid}/pos", async (Guid parentId, string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListPointOfSales(parentId, search, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/branches/{parentId:guid}/pos", async (Guid parentId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Command.CreatePointOfSale(parentId, data), cancellationToken);
                return result.IsFailure ? Failure(result) : Results.Created($"/api/v1/pos/{result.Value!.Id}", result.Value);
            });
            group.MapGet("/pos/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.GetPointOfSale(id), cancellationToken)));
            group.MapPut("/pos/{id:guid}", async (Guid id, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.UpdatePointOfSale(id, data), cancellationToken)));
            group.MapPatch("/pos/{id:guid}/status", async (Guid id, StatusRequest data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.SetPointOfSaleStatus(id, data.Status), cancellationToken)));
        }

        public sealed record StatusRequest(bool Status);
    }
}
