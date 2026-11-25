using BuildingBlocks.AspNetCore.Authentication;
using Carter;
using Identity.Contract.Services.V1.Identity;
using MediatR;

namespace Identity.Api.Controllers.v1.Identity
{
    public sealed class UserDirectoryApi : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/internal/v1/users/{userId:guid}/tenants/{tenantId:guid}",
                async (Guid userId, Guid tenantId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new UserBelongsToTenantQuery(userId, tenantId), cancellationToken);
                    return result.IsSuccess && result.Value ? Results.NoContent() : Results.NotFound();
                })
                .AddEndpointFilter<ServiceKeyFilter>()
                .ExcludeFromDescription();
        }
    }
}
