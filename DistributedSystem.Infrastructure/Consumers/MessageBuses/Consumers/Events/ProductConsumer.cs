using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Infrastructure.Consumers.Abstract.Messages;
using MediatR;

namespace DistributedSystem.Infrastructure.Consumers.MessageBuses.Consumers.Events
{
    public static class ProductConsumer
    {
        public class ProductCreatedConsumer : Consumer<DomainEvent.ProductCreated>
        {
            public ProductCreatedConsumer(ISender sender) : base(sender)
            {
            }
        }
        public class ProductDeletedConsumer : Consumer<DomainEvent.ProductDeleted>
        {
            public ProductDeletedConsumer(ISender sender) : base(sender)
            {
            }
        }

        public class ProductUpdatedConsumer : Consumer<DomainEvent.ProductUpdated>
        {
            public ProductUpdatedConsumer(ISender sender) : base(sender)
            {
            }
        }
    }
}