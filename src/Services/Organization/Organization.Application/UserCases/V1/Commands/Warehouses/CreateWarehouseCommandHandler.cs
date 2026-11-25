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
    public sealed class CreateWarehouseCommandHandler : ICommandHandler<Command.CreateWarehouse, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;
        private readonly IRepository<global::Organization.Domain.Entities.Branches.Branch> _parentRepository;

        public CreateWarehouseCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access,
            IRepository<global::Organization.Domain.Entities.Branches.Branch> parentRepository)
        {
            _repository = repository;
            _access = access;
            _parentRepository = parentRepository;
        }

        public async Task<Result<Response.Detail>> Handle(Command.CreateWarehouse request, CancellationToken cancellationToken)
        {
            if (!await _access.CanCreateAsync("Warehouse", request.BranchId, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            var parent = await _parentRepository.GetAsync(request.BranchId, cancellationToken);
            if (parent is null || !parent.Status)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            var entity = new Entity
            {
                TenantId = parent.TenantId,
                BranchId = parent.Id,
            };
            WarehouseMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, entity.BranchId, entity.Code, null, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            if (entity.IsDefault)
            {
                await _repository.ClearDefaultAsync(entity.TenantId, entity.BranchId, entity.Id, cancellationToken);
            }

            _repository.Add(entity);
            return Result.Success(WarehouseMapping.ToResponse(entity));
        }
    }
}
