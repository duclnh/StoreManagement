using AutoMapper;
using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Entities;
using DistributedSystem.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Application.UserCases.V1.Queries.Product
{
    public class GetProductsQueryHandler : IQueryHandler<Query.GetProductsQuery, List<Response.ProductResponse>>
    {
        private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepositoryBase;
        private readonly IMapper _mapper;

        public GetProductsQueryHandler(IRepositoryBase<Domain.Entities.Product, Guid> repositoryBase, IMapper mapper)
        {
            _productRepositoryBase = repositoryBase;
            _mapper = mapper;
        }

        public async Task<Result<List<Response.ProductResponse>>> Handle(Query.GetProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _productRepositoryBase.FindAll().ToListAsync(cancellationToken);
            var result = _mapper.Map<List<Response.ProductResponse>>(products);

            return Result.Success(result);
        }
    }
}
