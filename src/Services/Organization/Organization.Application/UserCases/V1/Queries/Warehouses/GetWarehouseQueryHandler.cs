using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Warehouses;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.Warehouses.Warehouse;

namespace Organization.Application.UserCases.V1.Queries.Warehouses
{
    public sealed class GetWarehouseQueryHandler : IQueryHandler<Query.GetWarehouse, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public GetWarehouseQueryHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Query.GetWarehouse request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("Warehouse", entity.Id, false, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            return Result.Success(WarehouseMapping.ToResponse(entity));
        }
    }
}
