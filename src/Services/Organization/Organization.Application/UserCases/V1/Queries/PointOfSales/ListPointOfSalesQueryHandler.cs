using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.PointOfSales;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Application.UserCases.V1.Queries.PointOfSales
{
    public sealed class ListPointOfSalesQueryHandler : IQueryHandler<Query.ListPointOfSales, Page<Response.Detail>>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public ListPointOfSalesQueryHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Page<Response.Detail>>> Handle(Query.ListPointOfSales request, CancellationToken cancellationToken)
        {
            if (!await _access.CanListAsync("PointOfSale", request.ParentId, cancellationToken))
            {
                return Result.Failure<Page<Response.Detail>>(Errors.Forbidden);
            }

            var items = await _repository.ListAsync(request.ParentId, request.Search, request.Page, request.PageSize, cancellationToken);
            var count = await _repository.CountAsync(request.ParentId, request.Search, cancellationToken);
            return Result.Success(new Page<Response.Detail>(items.Select(PointOfSaleMapping.ToResponse).ToArray(), request.Page, request.PageSize, count));
        }
    }
}
