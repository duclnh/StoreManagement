using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Infrastructure.Consumers.Abstract.Messages;
using DistributedSystem.Infrastructure.Consumers.Abstract.Repositories;
using DistributedSystem.Infrastructure.Consumers.Models;
using MediatR;

namespace DistributedSystem.Infrastructure.Consumers.MessageBuses.Consumers.Events
{
    public static class ProductConsumer
    {
        public class ProductCreatedConsumer : Consumer<DomainEvent.ProductCreated>
        {
            public ProductCreatedConsumer(ISender sender, IMongoRepository<EventProjection> mongoRepository) : base(sender, mongoRepository)
            {
            }
        }
        public class ProductDeletedConsumer : Consumer<DomainEvent.ProductDeleted>
        {
            public ProductDeletedConsumer(ISender sender, IMongoRepository<EventProjection> mongoRepository) : base(sender, mongoRepository)
            {
            }
        }

        public class ProductUpdatedConsumer : Consumer<DomainEvent.ProductUpdated>
        {
            public ProductUpdatedConsumer(ISender sender, IMongoRepository<EventProjection> mongoRepository) : base(sender, mongoRepository)
            {
            }
        }
    }
}