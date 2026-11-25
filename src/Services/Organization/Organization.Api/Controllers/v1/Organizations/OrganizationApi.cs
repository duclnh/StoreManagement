using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.Organizations;

namespace Organization.Api.Controllers.v1.Organizations
{
    public sealed class OrganizationApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-organizations")
                .MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/organizations", async (Guid? tenantId, string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListOrganizations(tenantId, search, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/organizations", async (Guid tenantId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Command.CreateOrganization(tenantId, data), cancellationToken);
                return result.IsFailure ? Failure(result) : Results.Created($"/api/v1/organizations/{result.Value!.Id}", result.Value);
            });
            group.MapGet("/organizations/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.GetOrganization(id), cancellationToken)));
            group.MapPut("/organizations/{id:guid}", async (Guid id, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.UpdateOrganization(id, data), cancellationToken)));
            group.MapPatch("/organizations/{id:guid}/status", async (Guid id, StatusRequest data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.SetOrganizationStatus(id, data.Status), cancellationToken)));
        }

        public sealed record StatusRequest(bool Status);
    }
}
