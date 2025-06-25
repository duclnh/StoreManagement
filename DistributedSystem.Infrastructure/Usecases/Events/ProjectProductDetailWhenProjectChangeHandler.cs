using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;

namespace DistributedSystem.Infrastructure.Usecases.Events
{
    internal class ProjectProductDetailWhenProjectChangeHandler : ICommandHandler<DomainEvent.ProductCreated>,
    ICommandHandler<DomainEvent.ProductUpdated>,
    ICommandHandler<DomainEvent.ProductDeleted>
    {
        public async Task<Result> Handle(DomainEvent.ProductCreated request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> Handle(DomainEvent.ProductUpdated request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> Handle(DomainEvent.ProductDeleted request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}