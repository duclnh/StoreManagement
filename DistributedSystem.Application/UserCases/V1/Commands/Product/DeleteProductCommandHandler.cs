using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Exceptions;
using DistributedSystem.Domain.Repositories;

namespace DistributedSystem.Application.UserCases.V1.Commands.Product
{
    public class DeleteProductCommandHandler : ICommandHandler<Command.DeleteProductCommand>
    {
        private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepositoryBase;

        public DeleteProductCommandHandler(IRepositoryBase<Domain.Entities.Product, Guid> repositoryBase)
        {
            _productRepositoryBase = repositoryBase;
        }

        public async Task<Result> Handle(Command.DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepositoryBase.FindByIdAsync(request.Id, cancellationToken);

            if (product is null)
                throw new ProductException.ProductNotFoundException(request.Id);

            _productRepositoryBase.Remove(product);

            return Result.Success();
        }
    }
}
