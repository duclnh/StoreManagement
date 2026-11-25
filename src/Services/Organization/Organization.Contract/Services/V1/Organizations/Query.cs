using BuildingBlocks.Contracts.Message;
using Organization.Contract.Shared;

namespace Organization.Contract.Services.V1.Organizations
{
    public static class Query
    {
        public sealed record GetOrganization(Guid Id) : IQuery<Response.Detail>;
        public sealed record ListOrganizations(Guid? ParentId = null, string? Search = null, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
    }
}
