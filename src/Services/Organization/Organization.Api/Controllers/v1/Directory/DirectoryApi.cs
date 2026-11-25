using Carter;
using MediatR;
using Organization.Api.Abstractions;
using BuildingBlocks.AspNetCore.Authentication;
using Organization.Contract.Services.V1.Directory;

namespace Organization.Api.Controllers.v1.Directory
{
    public sealed class DirectoryApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/internal/v1/tenants", async (ISender sender, CancellationToken cancellationToken) =>
                    Map(await sender.Send(new Query.ListDirectoryTenants(), cancellationToken)))
                .AddEndpointFilter<ServiceKeyFilter>()
                .ExcludeFromDescription();
            app.MapGet("/internal/v1/tenants/{tenantId:guid}/snapshot",
                async (Guid tenantId, ISender sender, CancellationToken cancellationToken) =>
                    Map(await sender.Send(new Query.GetTenantSnapshot(tenantId), cancellationToken)))
                .AddEndpointFilter<ServiceKeyFilter>()
                .ExcludeFromDescription();
        }
    }
}
