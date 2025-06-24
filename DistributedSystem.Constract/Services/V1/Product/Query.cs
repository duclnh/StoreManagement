using DistributedSystem.Constract.Abstraction.Message;
using static DistributedSystem.Constract.Services.V1.Product.Response;


namespace DistributedSystem.Constract.Services.V1.Product
{
    public static class Query
    {
        public record GetProductsQuery() : IQuery<List<ProductResponse>>;
        public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
    }
}
