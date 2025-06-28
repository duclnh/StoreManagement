using DistributedSystem.Infrastructure.Consumers.Attributes;
using DistributedSystem.Infrastructure.Consumers.Constants;

namespace DistributedSystem.Infrastructure.Consumers.Models
{
    [BsonCollection(TableNames.Product)]
    public class ProductProjection : Document
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}