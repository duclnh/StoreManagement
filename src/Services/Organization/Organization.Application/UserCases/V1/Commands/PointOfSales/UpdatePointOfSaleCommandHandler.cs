using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.PointOfSales;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Application.UserCases.V1.Commands.PointOfSales
{
    public sealed class UpdatePointOfSaleCommandHandler : ICommandHandler<Command.UpdatePointOfSale, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public UpdatePointOfSaleCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.UpdatePointOfSale request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("PointOfSale", entity.Id, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            PointOfSaleMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, entity.BranchId, entity.Code, entity.Id, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            return Result.Success(PointOfSaleMapping.ToResponse(entity));
        }
    }
}
