using MassTransit;
using MediatR;

namespace DistributedSystem.Constract.Abstraction.Message
{
    [ExcludeFromTopology]
    public interface IDomainEvent //: INotification
    {
        public Guid IdEvent { get; init; }
    }
}
