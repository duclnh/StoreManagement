using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Enumerations;
using static DistributedSystem.Constract.Services.V1.Product.Response;


namespace DistributedSystem.Constract.Services.V1.Product
{
    public static class Query
    {
        public record GetProductsQuery(string? SearchTerm, string? SortColumn, SortOrder? SortOrder, Dictionary<string,  SortOrder> SortColumnAndOrder, int PageIndex, int PageSize) : IQuery<PageResult<ProductResponse>>;
        public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
    }
}
