using System.Reflection.Metadata;
using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Infrastructure.Consumers.Abstract.Repositories;
using DistributedSystem.Infrastructure.Consumers.Models;

namespace DistributedSystem.Infrastructure.Usecases.Events
{
    internal class ProjectProductDetailWhenProjectChangeHandler : ICommandHandler<DomainEvent.ProductCreated>,
    ICommandHandler<DomainEvent.ProductUpdated>,
    ICommandHandler<DomainEvent.ProductDeleted>
    {
        private readonly IMongoRepository<ProductProjection> _mongoRepository;

        public ProjectProductDetailWhenProjectChangeHandler(IMongoRepository<ProductProjection> mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        public async Task<Result> Handle(DomainEvent.ProductCreated request, CancellationToken cancellationToken)
        {
            var product = new ProductProjection()
            {
                DocumentId = request.Id,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
            };

            await _mongoRepository.InsertOneAsync(product);

            return Result.Success();
        }

        public async Task<Result> Handle(DomainEvent.ProductUpdated request, CancellationToken cancellationToken)
        {
            var product = await _mongoRepository.FindOneAsync(document => document.DocumentId == request.Id)
            ?? throw new ArgumentNullException();

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.ModifiedOnUtc = DateTime.UtcNow;

            await _mongoRepository.ReplaceOneAsync(product);

            return Result.Success();
        }

        public async Task<Result> Handle(DomainEvent.ProductDeleted request, CancellationToken cancellationToken)
        {
            var product = await _mongoRepository.FindOneAsync(document => document.DocumentId == request.Id)
              ?? throw new ArgumentNullException();

            await _mongoRepository.DeleteOneAsync(x => x.DocumentId == request.Id);

            return Result.Success();
        }
    }
}