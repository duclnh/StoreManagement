using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Tenants;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.Tenants.Tenant;

namespace Organization.Application.UserCases.V1.Commands.Tenants
{
    public sealed class UpdateTenantCommandHandler : ICommandHandler<Command.UpdateTenant, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public UpdateTenantCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.UpdateTenant request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("Tenant", entity.Id, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            TenantMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, null, entity.Code, entity.Id, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            return Result.Success(TenantMapping.ToResponse(entity));
        }
    }
}
