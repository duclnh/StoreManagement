using AutoMapper;
using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Entities;
using DistributedSystem.Domain.Exceptions;
using DistributedSystem.Domain.Repositories;
using DistributedSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistributedSystem.Application.UserCases.V1.Queries.Product
{
    public class GetProductByIdQueryHandler : IQueryHandler<Query.GetProductByIdQuery, Response.ProductResponse>
    {
        private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepositoryBase;
        private readonly IRepositoryBaseDbContext<ApplicationDbContext, Domain.Entities.Product, Guid> _productRepository;

        private readonly IMapper _mapper;

        public GetProductByIdQueryHandler(
            IRepositoryBase<Domain.Entities.Product, Guid> productRepositoryBase,
             IRepositoryBaseDbContext<ApplicationDbContext,Domain.Entities.Product, Guid> productRepository
            , IMapper mapper)
        {
            _productRepositoryBase = productRepositoryBase;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<Result<Response.ProductResponse>> Handle(Query.GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var productBase = await _productRepositoryBase.FindByIdAsync(request.Id, cancellationToken)
                ?? throw new ProductException.ProductNotFoundException(request.Id);

            var product2 = await _productRepository.FindByIdAsync(request.Id, cancellationToken)
                ?? throw new ProductException.ProductNotFoundException(request.Id);


            var result = _mapper.Map<Response.ProductResponse>(productBase);

            return Result.Success(result);
        }
    }
}
