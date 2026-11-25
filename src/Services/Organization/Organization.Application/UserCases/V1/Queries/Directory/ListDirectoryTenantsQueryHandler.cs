using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Contract.Services.V1.Directory;
using Organization.Domain.Repositories;

namespace Organization.Application.UserCases.V1.Queries.Directory
{
    public sealed class ListDirectoryTenantsQueryHandler : IQueryHandler<Query.ListDirectoryTenants, IReadOnlyList<Response.TenantScope>>
    {
        private readonly IScopeDirectoryRepository _directory;

        public ListDirectoryTenantsQueryHandler(IScopeDirectoryRepository directory)
        {
            _directory = directory;
        }

        public async Task<Result<IReadOnlyList<Response.TenantScope>>> Handle(Query.ListDirectoryTenants request, CancellationToken cancellationToken)
        {
            var tenants = await _directory.GetTenantsAsync(cancellationToken);
            return Result.Success<IReadOnlyList<Response.TenantScope>>(tenants.Select(x =>
                new Response.TenantScope(x.Id, x.Code, x.Name, x.Status, x.CreatedAt)).ToArray());
        }
    }
}
