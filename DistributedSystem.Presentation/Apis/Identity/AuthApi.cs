
using Carter;
using DistributedSystem.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using static DistributedSystem.Constract.Services.V1.Identity.Query;

namespace DistributedSystem.Presentation.Apis.Identity
{
    public class AuthApi : ApiEndpoint, ICarterModule
    {
        private const string BaseUrl = "/api/v{version:apiVersion}/auths";

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group1 = app.NewVersionedApi("authentications")
                .MapGroup(BaseUrl)
                .HasApiVersion(1)
                .RequireAuthorization();

            group1.MapPost("login", AuthenicationV1).AllowAnonymous();
        }

        private async Task<IResult> AuthenicationV1(ISender sender, [FromBody] Login login)
        {
            var result = await sender.Send(login);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }
    }
}
