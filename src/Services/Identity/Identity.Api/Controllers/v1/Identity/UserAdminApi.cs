using Carter;
using Identity.Presentation.Abstractions;
using Identity.Api.Authorization;
using Identity.Contract.Abstraction.Shared;
using IdentityContracts = global::Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
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
                .RequireAuthorization();

            group.MapGet("users", GetUsers).RequirePermission("Identity.Users", "View");
            group.MapGet("users/{userId:guid}/roles", GetUserRoles).RequirePermission("Identity.Users", "View");
            group.MapPost("users", CreateUser).RequirePermission("Identity.Users", "Create");
            group.MapGet("users/{userId:guid}", GetUser).RequirePermission("Identity.Users", "View");
            group.MapPut("users/{userId:guid}", UpdateUser).RequirePermission("Identity.Users", "Update");
            group.MapPatch("users/{userId:guid}/status", SetUserActive).RequirePermission("Identity.Users", "Update");
            group.MapPut("users/{userId:guid}/roles", AssignUserRoles).RequirePermission("Identity.Users", "Assign");
            group.MapGet("roles", GetRoles).RequirePermission("Identity.Roles", "View");
            group.MapGet("scopes", GetIdentityScopes).RequirePermission("Identity.Users", "View");
            group.MapGet("tenants", GetIdentityTenants)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("List identity tenants")
                .WithDescription("Returns all tenants, including inactive tenants, for platform administration.")
                .Produces<List<IdentityContracts.Response.IdentityTenant>>();
            group.MapGet("tenants/{tenantId:guid}/scopes", GetTenantScopes)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("List tenant, organization, and branch scopes")
                .WithDescription("Returns active and inactive scopes in the tenant for platform administration.")
                .Produces<List<IdentityContracts.Response.IdentityAdminScope>>();
            group.MapGet("scopes/{scopeId:guid}", GetIdentityScope)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Get an identity scope")
                .Produces<IdentityContracts.Response.IdentityScope>()
                .Produces(StatusCodes.Status404NotFound);
            group.MapPost("tenants", CreateIdentityTenant)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Create an identity tenant")
                .Produces<IdentityContracts.Response.IdentityScope>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status409Conflict);
            group.MapPut("tenants/{tenantId:guid}/status", SetIdentityTenantStatus)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Set tenant active status")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound);
            group.MapPost("tenants/{tenantId:guid}/organizations", CreateIdentityOrganization)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Create an organization in a tenant")
                .Produces<IdentityContracts.Response.IdentityScope>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);
            group.MapPut("tenants/{tenantId:guid}/organizations/{organizationId:guid}/status", SetIdentityOrganizationStatus)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Set organization active status")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound);
            group.MapPost("tenants/{tenantId:guid}/organizations/{organizationId:guid}/branches", CreateIdentityBranch)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Create a branch in an organization")
                .Produces<IdentityContracts.Response.IdentityScope>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);
            group.MapPut("tenants/{tenantId:guid}/organizations/{organizationId:guid}/branches/{branchId:guid}/status", SetIdentityBranchStatus)
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .WithSummary("Set branch active status")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound);
            group.MapPost("roles", CreateRole).RequirePermission("Identity.Roles", "Create");
            group.MapPut("roles/{roleId:guid}", UpdateRole).RequirePermission("Identity.Roles", "Update");
            group.MapDelete("roles/{roleId:guid}", DeleteRole).RequirePermission("Identity.Roles", "Delete");
            group.MapGet("audit-events", GetSecurityAuditEvents).RequirePermission("Identity.Security", "View");
            group.MapGet("roles/{roleId:guid}/permissions", GetRolePermissions).RequirePermission("Identity.Roles", "Assign");
            group.MapPut("roles/{roleId:guid}/permissions", SetRolePermissions).RequirePermission("Identity.Roles", "Assign");
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

        private async Task<IResult> GetUserRoles(
            ISender sender,
            IIdentityTenantContext tenantContext,
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (!tenantContext.TenantId.HasValue || !tenantContext.RequestedScopeId.HasValue)
                return Results.BadRequest(new { error = "A valid tenant and scope context are required." });

            var result = await sender.Send(
                new GetUserRoles(userId, tenantContext.TenantId.Value, tenantContext.RequestedScopeId.Value),
                cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetIdentityScopes(ISender sender, IIdentityTenantContext tenantContext, CancellationToken cancellationToken)
        {
            if (!tenantContext.TenantId.HasValue)
                return Results.BadRequest(new { error = "A valid tenant context is required." });

            var result = await sender.Send(new GetIdentityScopes(tenantContext.TenantId.Value), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetIdentityTenants(ISender sender, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetIdentityTenants(), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetTenantScopes(
            ISender sender,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetTenantScopes(tenantId), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetIdentityScope(ISender sender, Guid scopeId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetIdentityScope(scopeId), cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);
            return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
        }

        private async Task<IResult> CreateIdentityTenant(
            ISender sender,
            [FromBody] IdentityScopeNameRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new CreateIdentityTenant(request.Name), cancellationToken);
            if (result.IsFailure)
                return IdentityScopeFailure(result.Error!);
            var scope = result.Value ?? throw new InvalidOperationException("Tenant creation returned no scope.");
            return Results.Created($"/api/v1/admin/scopes/{scope.Id}", scope);
        }

        private async Task<IResult> CreateIdentityOrganization(
            ISender sender,
            Guid tenantId,
            [FromBody] IdentityScopeNameRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new CreateIdentityOrganization(tenantId, request.Name), cancellationToken);
            if (result.IsFailure)
                return IdentityScopeFailure(result.Error!);
            var scope = result.Value ?? throw new InvalidOperationException("Organization creation returned no scope.");
            return Results.Created($"/api/v1/admin/scopes/{scope.Id}", scope);
        }

        private async Task<IResult> CreateIdentityBranch(
            ISender sender,
            Guid tenantId,
            Guid organizationId,
            [FromBody] IdentityScopeNameRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new CreateIdentityBranch(tenantId, organizationId, request.Name),
                cancellationToken);
            if (result.IsFailure)
                return IdentityScopeFailure(result.Error!);
            var scope = result.Value ?? throw new InvalidOperationException("Branch creation returned no scope.");
            return Results.Created($"/api/v1/admin/scopes/{scope.Id}", scope);
        }

        private async Task<IResult> SetIdentityTenantStatus(
            ISender sender,
            Guid tenantId,
            [FromBody] IdentityScopeStatusRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new SetIdentityTenantStatus(tenantId, request.IsActive),
                cancellationToken);
            return result.IsFailure ? IdentityScopeFailure(result.Error!) : Results.NoContent();
        }

        private async Task<IResult> SetIdentityOrganizationStatus(
            ISender sender,
            Guid tenantId,
            Guid organizationId,
            [FromBody] IdentityScopeStatusRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new SetIdentityOrganizationStatus(tenantId, organizationId, request.IsActive),
                cancellationToken);
            return result.IsFailure ? IdentityScopeFailure(result.Error!) : Results.NoContent();
        }

        private async Task<IResult> SetIdentityBranchStatus(
            ISender sender,
            Guid tenantId,
            Guid organizationId,
            Guid branchId,
            [FromBody] IdentityScopeStatusRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new SetIdentityBranchStatus(tenantId, organizationId, branchId, request.IsActive),
                cancellationToken);
            return result.IsFailure ? IdentityScopeFailure(result.Error!) : Results.NoContent();
        }

        private static IResult IdentityScopeFailure(Error error) =>
            error.Code switch
            {
                "Identity.Scope.NotFound" => Results.NotFound(new ProblemDetails
                {
                    Title = "Identity scope not found",
                    Detail = error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Type = error.Code
                }),
                "Identity.Scope.Conflict" => Results.Conflict(new ProblemDetails
                {
                    Title = "Identity scope conflict",
                    Detail = error.Message,
                    Status = StatusCodes.Status409Conflict,
                    Type = error.Code
                }),
                _ => Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid identity scope request",
                    Detail = error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Type = error.Code
                })
            };

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
            IIdentityTenantContext tenantContext,
            ClaimsPrincipal actor,
            Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetActorId(actor, out var actorId))
                return Results.Unauthorized();
            if (!tenantContext.TenantId.HasValue || !tenantContext.RequestedScopeId.HasValue)
                return Results.BadRequest(new { error = "A valid tenant and scope context are required." });

            var result = await sender.Send(new AssignUserRoles(
                userId,
                actorId,
                tenantContext.TenantId.Value,
                tenantContext.RequestedScopeId.Value,
                request.Roles), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
        }

        private async Task<IResult> GetRoles(ISender sender, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetRoles(), cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetRolePermissions(
            ISender sender,
            IIdentityTenantContext tenantContext,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            if (!tenantContext.TenantId.HasValue || !tenantContext.RequestedScopeId.HasValue)
                return Results.BadRequest(new { error = "A valid tenant and scope context are required." });

            var result = await sender.Send(
                new GetRolePermissions(roleId, tenantContext.TenantId.Value, tenantContext.RequestedScopeId.Value),
                cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> GetSecurityAuditEvents(
            ISender sender,
            CancellationToken cancellationToken,
            [FromQuery] DateTimeOffset? fromUtc = null,
            [FromQuery] DateTimeOffset? toUtc = null,
            [FromQuery] string? eventType = null,
            [FromQuery] string? outcome = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await sender.Send(
                new GetSecurityAuditEvents(fromUtc, toUtc, eventType, outcome, userId, page, pageSize),
                cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
        }

        private async Task<IResult> SetRolePermissions(
            ISender sender,
            IIdentityTenantContext tenantContext,
            Guid roleId,
            [FromBody] SetRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Permissions is null || request.Permissions.Any(permission => permission is null))
                return Results.BadRequest(new { error = "A permission list is required." });
            if (!tenantContext.TenantId.HasValue || !tenantContext.RequestedScopeId.HasValue)
                return Results.BadRequest(new { error = "A valid tenant and scope context are required." });

            var result = await sender.Send(
                new SetRolePermissionsCommand(
                    roleId,
                    tenantContext.TenantId.Value,
                    tenantContext.RequestedScopeId.Value,
                    request.Permissions.Select(permission => new PermissionGrant(
                        permission!.FunctionId,
                        permission.ActionId)).ToList()),
                cancellationToken);
            return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
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

        public sealed record SetRolePermissionsRequest(List<PermissionGrantRequest?>? Permissions);
        public sealed record PermissionGrantRequest(string FunctionId, string ActionId);

        public sealed record IdentityScopeNameRequest
        {
            [Required, StringLength(150, MinimumLength = 1)]
            public required string Name { get; init; }
        }

        public sealed record IdentityScopeStatusRequest
        {
            public required bool IsActive { get; init; }
        }
    }
}