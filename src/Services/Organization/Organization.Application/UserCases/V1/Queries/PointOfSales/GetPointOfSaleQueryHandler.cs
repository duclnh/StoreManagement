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
    public sealed class GetPointOfSaleQueryHandler : IQueryHandler<Query.GetPointOfSale, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public GetPointOfSaleQueryHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Query.GetPointOfSale request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("PointOfSale", entity.Id, false, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            return Result.Success(PointOfSaleMapping.ToResponse(entity));
        }
    }
}
