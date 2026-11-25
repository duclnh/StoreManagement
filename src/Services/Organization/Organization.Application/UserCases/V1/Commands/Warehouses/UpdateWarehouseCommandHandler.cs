using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Warehouses;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.Warehouses.Warehouse;

namespace Organization.Application.UserCases.V1.Commands.Warehouses
{
    public sealed class UpdateWarehouseCommandHandler : ICommandHandler<Command.UpdateWarehouse, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public UpdateWarehouseCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.UpdateWarehouse request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("Warehouse", entity.Id, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            WarehouseMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, entity.BranchId, entity.Code, entity.Id, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            if (entity.IsDefault)
            {
                await _repository.ClearDefaultAsync(entity.TenantId, entity.BranchId, entity.Id, cancellationToken);
            }

            return Result.Success(WarehouseMapping.ToResponse(entity));
        }
    }
}
