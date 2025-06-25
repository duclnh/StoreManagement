using MassTransit;
using MediatR;

namespace DistributedSystem.Infrastructure.Consumers.Abstract.Messages
{
    public abstract class Consumer<TMessage> : IConsumer<TMessage>
        where TMessage : class,
        Constract.Abstraction.Message.IDomainEvent
    {
        private readonly ISender _sender;

        protected Consumer(ISender sender)
        {
            _sender = sender;
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
           await _sender.Send(context.Message); //Don't call _dbContext.SaveChangeAsync in it
        }
    }
}