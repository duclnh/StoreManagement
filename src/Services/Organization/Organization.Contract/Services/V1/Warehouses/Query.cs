using BuildingBlocks.Contracts.Message;
using Organization.Contract.Shared;

namespace Organization.Contract.Services.V1.Warehouses
{
    public static class Query
    {
        public sealed record GetWarehouse(Guid Id) : IQuery<Response.Detail>;
        public sealed record ListWarehouses(Guid? ParentId = null, string? Search = null, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
    }
}
