using DistributedSystem.Infrastructure.Consumers.Abstract.Repositories;
using DistributedSystem.Infrastructure.Consumers.Models;
using MassTransit;
using MediatR;

namespace DistributedSystem.Infrastructure.Consumers.Abstract.Messages
{
    public abstract class Consumer<TMessage> : IConsumer<TMessage>
        where TMessage : class,
        Constract.Abstraction.Message.IDomainEvent
    {
        private readonly ISender _sender;
        private readonly IMongoRepository<EventProjection> _mongoRepository;

        protected Consumer(ISender sender, IMongoRepository<EventProjection> mongoRepository)
        {
            _sender = sender;
            _mongoRepository = mongoRepository;
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            //var event
            var eventProjection = await _mongoRepository.FindOneAsync(x => x.EventId == context.Message.IdEvent);
            if (eventProjection is null)
            {
                await _sender.Send(context.Message); //Don't call _dbContext.SaveChangeAsync in it

                eventProjection = new EventProjection()
                {
                    EventId = context.Message.IdEvent,
                    Name = context.Message.GetType().Name,
                    Type = context.Message.GetType().Name,
                };

                await _mongoRepository.InsertOneAsync(eventProjection);
            }
        }
    }
}