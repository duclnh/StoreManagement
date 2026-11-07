using Carter;
using Identity.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Identity.Contract.Services.V1.Identity.Command;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Presentation.Apis.Identity
{
    public sealed class UserAdminApi : ApiEndpoint, ICarterModule
    {
        private const string BaseUrl = "/api/v{version:apiVersion}/admin";

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("identity-administration")
                .MapGroup(BaseUrl)
                .HasApiVersion(1)
                .RequireAuthorization("IdentityAdmin");

            group.MapGet("users", GetUsersV1);
            group.MapGet("users/{userId:guid}", GetUserV1);
            group.MapPut("users/{userId:guid}", UpdateUserV1);
            group.MapPatch("users/{userId:guid}/status", SetUserActiveV1);
            group.MapPut("users/{userId:guid}/roles", AssignUserRolesV1);
            group.MapGet("roles", GetRolesV1);
            group.MapPost("roles", CreateRoleV1);
            group.MapPut("roles/{roleId:guid}", UpdateRoleV1);
            group.MapDelete("roles/{roleId:guid}", DeleteRoleV1);
        }

        private async Task<IResult> GetUsersV1(
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var result = await sender.Send(new GetUsers(search, page, pageSize), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetUserV1(ISender sender, Guid userId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUser(userId), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> UpdateUserV1(
            ISender sender,
            ClaimsPrincipal actor,
            Guid userId,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetActorId(actor, out var actorId))
                return Results.Unauthorized();

            var result = await sender.Send(new UpdateUserProfile(userId, actorId, request.FirstName, request.LastName), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> SetUserActiveV1(
            ISender sender,
            ClaimsPrincipal actor,
            Guid userId,
            [FromBody] SetUserActiveRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetActorId(actor, out var actorId))
                return Results.Unauthorized();

            var result = await sender.Send(new SetUserActive(userId, actorId, request.IsActive), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> AssignUserRolesV1(
            ISender sender,
            ClaimsPrincipal actor,
            Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetActorId(actor, out var actorId))
                return Results.Unauthorized();

            var result = await sender.Send(new AssignUserRoles(userId, actorId, request.Roles), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> GetRolesV1(ISender sender, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetRoles(), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> CreateRoleV1(
            ISender sender,
            [FromBody] CreateRole request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Created($"/api/v1/admin/roles/{result.Value}", result.Value);
        }

        private async Task<IResult> UpdateRoleV1(
            ISender sender,
            Guid roleId,
            [FromBody] UpdateRoleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new UpdateRole(roleId, request.Name, request.RoleCode, request.Description), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> DeleteRoleV1(ISender sender, Guid roleId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new DeleteRole(roleId), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private static bool TryGetActorId(ClaimsPrincipal actor, out Guid actorId) =>
            Guid.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    }
}