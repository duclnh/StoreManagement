using Carter;
using MediatR;
using Organization.Api.Abstractions;
using Organization.Contract.Services.V1.Members;

namespace Organization.Api.Controllers.v1.Members
{
    public sealed class MemberApi : ApiEndpoint, ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.NewVersionedApi("organization-members").MapGroup("/api/v{version:apiVersion}").HasApiVersion(1).RequireAuthorization();
            group.MapGet("/organizations/{organizationId:guid}/members", async (Guid organizationId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListOrganizationMembers(organizationId, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/organizations/{organizationId:guid}/members", async (Guid organizationId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.AddOrganizationMember(organizationId, data), cancellationToken)));
            group.MapDelete("/organizations/{organizationId:guid}/members/{userId:guid}", async (Guid organizationId, Guid userId, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.RemoveOrganizationMember(organizationId, userId), cancellationToken)));
            group.MapGet("/branches/{branchId:guid}/members", async (Guid branchId, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Query.ListBranchMembers(branchId, page ?? 1, pageSize ?? 20), cancellationToken)));
            group.MapPost("/branches/{branchId:guid}/members", async (Guid branchId, Command.Data data, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.AssignBranchMember(branchId, data), cancellationToken)));
            group.MapDelete("/branches/{branchId:guid}/members/{userId:guid}", async (Guid branchId, Guid userId, ISender sender, CancellationToken cancellationToken) =>
                Map(await sender.Send(new Command.RemoveBranchMember(branchId, userId), cancellationToken)));
        }
    }
}
