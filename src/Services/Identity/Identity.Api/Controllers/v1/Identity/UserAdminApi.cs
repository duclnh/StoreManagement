using Carter;
using Identity.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Identity.Contract.Services.V1.Identity.Command;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Presentation.Apis.V1.Identity
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

            group.MapGet("users", GetUsers);
            group.MapPost("users", CreateUser);
            group.MapGet("users/{userId:guid}", GetUser);
            group.MapPut("users/{userId:guid}", UpdateUser);
            group.MapPatch("users/{userId:guid}/status", SetUserActive);
            group.MapPut("users/{userId:guid}/roles", AssignUserRoles);
            group.MapGet("roles", GetRoles);
            group.MapPost("roles", CreateRole);
            group.MapPut("roles/{roleId:guid}", UpdateRole);
            group.MapDelete("roles/{roleId:guid}", DeleteRole);
        }

        private async Task<IResult> GetUsers(
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var result = await sender.Send(new GetUsers(search, page, pageSize), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetUser(ISender sender, Guid userId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUser(userId), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> CreateUser(
            ISender sender,
            [FromBody] CreateManagedUserRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateManagedUser(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Roles);
            var result = await sender.Send(command, cancellationToken);
            return result.IsFailure
                ? HandlerFailure(result)
                : Results.Created($"/api/v1/admin/users/{result.Value}", result.Value);
        }

        private async Task<IResult> UpdateUser(
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

        private async Task<IResult> SetUserActive(
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

        private async Task<IResult> AssignUserRoles(
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

        private async Task<IResult> GetRoles(ISender sender, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetRoles(), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> CreateRole(
            ISender sender,
            [FromBody] CreateRole request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Created($"/api/v1/admin/roles/{result.Value}", result.Value);
        }

        private async Task<IResult> UpdateRole(
            ISender sender,
            Guid roleId,
            [FromBody] UpdateRoleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new UpdateRole(roleId, request.Name, request.RoleCode, request.Description), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> DeleteRole(ISender sender, Guid roleId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new DeleteRole(roleId), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private static bool TryGetActorId(ClaimsPrincipal actor, out Guid actorId) =>
            Guid.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    }
}