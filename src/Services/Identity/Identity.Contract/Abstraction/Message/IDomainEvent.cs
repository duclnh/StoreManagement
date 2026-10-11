using MassTransit;
using MediatR;

namespace Identity.Contract.Abstraction.Message
{
    [ExcludeFromTopology]
    public interface IDomainEvent //: INotification
    {
        public Guid IdEvent { get; init; }
    }
}


