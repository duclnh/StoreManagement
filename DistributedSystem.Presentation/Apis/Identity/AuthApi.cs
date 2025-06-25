
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

            group1.MapPost("login", AuthenticationV1).AllowAnonymous();
            group1.MapPost("revoke", RevokeTokenV1).AllowAnonymous();
            group1.MapPost("refresh", RefreshTokenV1).AllowAnonymous();

        }

        private async Task<IResult> AuthenticationV1(ISender sender, [FromBody] Login login)
        {
            var result = await sender.Send(login);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RevokeTokenV1(ISender sender, [FromBody] Login login)
        {
            var result = await sender.Send(login);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

         private async Task<IResult> RefreshTokenV1(ISender sender, [FromBody] Login login)
        {
            var result = await sender.Send(login);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }
    }
}
