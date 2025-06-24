using DistributedSystem.Constract.Abstraction.Message;


namespace DistributedSystem.Constract.Services.V1.Product
{
    public static class DomainEvent
    {
        public record ProductCreated(Guid IdEvent, Guid Id, string Name, decimal Price, string Description) : IDomainEvent;
        public record ProductUpdated(Guid IdEvent, Guid Id, string Name, decimal Price, string Description) : IDomainEvent;
        public record ProductDeleted(Guid IdEvent, Guid Id) : IDomainEvent;
    }
}
