using BuildingBlocks.Contracts.Message;
using Organization.Contract.Shared;

namespace Organization.Contract.Services.V1.Members
{
    public static class Query
    {
        public sealed record ListOrganizationMembers(Guid OrganizationId, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
        public sealed record ListBranchMembers(Guid BranchId, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
    }
}
