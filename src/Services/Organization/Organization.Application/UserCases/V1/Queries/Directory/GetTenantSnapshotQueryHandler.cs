using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Domain.Repositories;
using Organization.Contract.Services.V1.Directory;

namespace Organization.Application.UserCases.V1.Queries.Directory
{
    public sealed class GetTenantSnapshotQueryHandler : IQueryHandler<Query.GetTenantSnapshot, Response.Snapshot>
    {
        private readonly IScopeDirectoryRepository _directory;

        public GetTenantSnapshotQueryHandler(IScopeDirectoryRepository directory)
        {
            _directory = directory;
        }

        public async Task<Result<Response.Snapshot>> Handle(Query.GetTenantSnapshot request, CancellationToken cancellationToken)
        {
            var snapshot = await _directory.GetSnapshotAsync(request.TenantId, cancellationToken);
            return snapshot is null ? Result.Failure<Response.Snapshot>(Errors.NotFound) : Result.Success(new Response.Snapshot(
                new Response.TenantScope(snapshot.Tenant.Id, snapshot.Tenant.Code, snapshot.Tenant.Name, snapshot.Tenant.Status, snapshot.Tenant.CreatedAt),
                snapshot.Organizations.Select(x => new Response.OrganizationScope(x.Id, x.TenantId, x.Name, x.Status)).ToArray(),
                snapshot.Branches.Select(x => new Response.BranchScope(x.Id, x.OrganizationId, x.Name, x.Status)).ToArray()));
        }
    }
}
