using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Application.UserCases.V1.Commands.Product
{
    public class CreateProductCommandHandler : ICommandHandler<Command.CreateProductCommand>
    {
        private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepositoryBase;

        public CreateProductCommandHandler(IRepositoryBase<Domain.Entities.Product, Guid> repositoryBase)
        {
            _productRepositoryBase = repositoryBase;
        }

        public async Task<Result> Handle(Command.CreateProductCommand request, CancellationToken cancellationToken)
        {
            var product = Domain.Entities.Product.CreateProduct(
                    Guid.NewGuid(),
                    request.Name,
                    request.Price,
                    request.Description
                );

            _productRepositoryBase.Add(product);

            return Result.Success();
        }
    }
}
