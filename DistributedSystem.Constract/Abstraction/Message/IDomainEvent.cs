using MediatR;

namespace DistributedSystem.Constract.Abstraction.Message
{
    public interface IDomainEvent : INotification
    {
        public Guid IdEvent { get; init; }
    }
}
