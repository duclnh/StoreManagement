using BuildingBlocks.Contracts.Message;
using Organization.Contract.Shared;

namespace Organization.Contract.Services.V1.PointOfSales
{
    public static class Query
    {
        public sealed record GetPointOfSale(Guid Id) : IQuery<Response.Detail>;
        public sealed record ListPointOfSales(Guid? ParentId = null, string? Search = null, int Page = 1, int PageSize = 20) : IQuery<Page<Response.Detail>>;
    }
}
