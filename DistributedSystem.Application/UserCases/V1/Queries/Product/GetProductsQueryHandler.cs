using AutoMapper;
using DistributedSystem.Constract.Abstraction.Message;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Enumerations;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Entities;
using DistributedSystem.Domain.Repositories;
using DistributedSystem.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static MassTransit.ValidationResultExtensions;
using Result = DistributedSystem.Constract.Abstraction.Shared.Result;

namespace DistributedSystem.Application.UserCases.V1.Queries.Product
{
    public class GetProductsQueryHandler : IQueryHandler<Query.GetProductsQuery, PageResult<Response.ProductResponse>>
    {
        private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepositoryBase;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;

        public GetProductsQueryHandler(IRepositoryBase<Domain.Entities.Product, Guid> repositoryBase, IMapper mapper, ApplicationDbContext applicationDbContext)
        {
            _productRepositoryBase = repositoryBase;
            _mapper = mapper;
            _context = applicationDbContext;
        }

        public async Task<Result<PageResult<Response.ProductResponse>>> Handle(Query.GetProductsQuery request, CancellationToken cancellationToken)
        {
            //var products = await _productRepositoryBase.FindAll().ToListAsync(cancellationToken);
            //var result = _mapper.Map<List<Response.ProductResponse>>(products);

            //var productQuery = string.IsNullOrEmpty(request.SearchTerm)
            //        ? _productRepositoryBase.FindAll() : _productRepositoryBase.FindAll(x => x.Name.Contains(request.SearchTerm) ||  x.Description.Contains(request.SearchTerm));

            //productQuery = request.SortOrder == SortOrder.Descending ? productQuery.OrderByDescending(GetSortProperty(request)) : productQuery.OrderBy(keySelector);


            if (request.SortColumnAndOrder.Any())
            {
                var PageIndex = request.PageIndex <= 0 ? PageResult<Domain.Entities.Product>.DefaultPageIndex : request.PageIndex;
                var PageSize = request.PageSize <= 0 ? PageResult<Domain.Entities.Product>.DefaultPageSize
                        : request.PageSize > PageResult<Domain.Entities.Product>.UpperPageSize
                        ? PageResult<Domain.Entities.Product>.UpperPageSize : request.PageSize;

                var productQuery = string.IsNullOrEmpty(request.SearchTerm)
                    ? $@"SELECT * FROM {nameof(Domain.Entities.Product)} ORDER BY "
                    : $@"SELECT * FROM {nameof(Domain.Entities.Product)}
                        WHERE {nameof(Domain.Entities.Product.Name)} LIKE '%{request.SearchTerm}%' 
                        OR {nameof(Domain.Entities.Product.DeleteProduct)} LIKE '%{request.SearchTerm}%' 
                        ORDER BY ";
                foreach (var item in request.SortColumnAndOrder)
                {
                    productQuery += item.Value == SortOrder.Descending
                                    ? $"{item.Key} DESC, "
                                    : $"{item.Key} ASC, ";
                }

                productQuery = productQuery.Remove(productQuery.Length - 2);

                productQuery += $" OFFSET {(PageIndex - 1) * PageSize} ROWS FETCH NEXT {PageSize} ROWS ONLY";

                var products = await _context.Products.FromSqlRaw(productQuery).ToListAsync(cancellationToken);

                var totalCount = await _context.Products.CountAsync(cancellationToken);

                var productPagedResult = PageResult<Domain.Entities.Product>.Create(products, PageIndex, PageSize, totalCount);
                var result = _mapper.Map<PageResult<Response.ProductResponse>>(productPagedResult);
                return Result.Success(result);
            }
            else
            {
                var productQuery = string.IsNullOrEmpty(request.SearchTerm)
                        ? _productRepositoryBase.FindAll() : _productRepositoryBase.FindAll(x => x.Name.Contains(request.SearchTerm) ||  x.Description.Contains(request.SearchTerm));


                productQuery = request.SortOrder == SortOrder.Descending
                    ? productQuery.OrderByDescending(GetSortProperty(request))
                    : productQuery.OrderBy(GetSortProperty(request));

                var productPagedResult = await PageResult<Domain.Entities.Product>.CreateAsync(productQuery, request.PageIndex, request.PageSize);

                var result = _mapper.Map<PageResult<Response.ProductResponse>>(productPagedResult);

                return Result.Success(result);
            }

            //var products = await productQuery.ToListAsync(cancellationToken);

            //var result = _mapper.Map<List<Response.ProductResponse>>(products);

            //return Result.Success(result);
        }

        private Expression<Func<Domain.Entities.Product, object>> GetSortProperty(Query.GetProductsQuery request)
        => request.SortColumn?.ToLower() switch
        {
            "description" => product => product.Description,
            "price" => product => product.Price,
            "name" => product => product.Name,
            _ => product => product.Id
        };
    }
}
