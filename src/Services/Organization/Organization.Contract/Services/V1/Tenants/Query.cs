using BuildingBlocks.Contracts.Message;
using Organization.Contract.Shared;

namespace Organization.Contract.Services.V1.Tenants
{
    public static class Query
    {
        public sealed record GetTenant(Guid Id) : IQuery<Response.Detail>;
        public sealed record ListTenants(Guid? ParentId = null, string? Search = null, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
    }
}
