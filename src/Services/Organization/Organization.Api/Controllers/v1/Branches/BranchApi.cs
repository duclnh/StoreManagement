using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.Branches;

namespace Organization.Api.Controllers.v1.Branches
{
    public sealed class BranchApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-branches")
                .MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/organizations/{parentId:guid}/branches", async (Guid parentId, string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListBranches(parentId, search, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/organizations/{parentId:guid}/branches", async (Guid parentId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Command.CreateBranch(parentId, data), cancellationToken);
                return result.IsFailure ? Failure(result) : Results.Created($"/api/v1/branches/{result.Value!.Id}", result.Value);
            });
            group.MapGet("/branches/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.GetBranch(id), cancellationToken)));
            group.MapPut("/branches/{id:guid}", async (Guid id, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.UpdateBranch(id, data), cancellationToken)));
            group.MapPatch("/branches/{id:guid}/status", async (Guid id, StatusRequest data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.SetBranchStatus(id, data.Status), cancellationToken)));
        }

        public sealed record StatusRequest(bool Status);
    }
}
