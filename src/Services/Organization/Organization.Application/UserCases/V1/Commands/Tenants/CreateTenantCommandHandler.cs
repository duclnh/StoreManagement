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
    public sealed class CreateTenantCommandHandler : ICommandHandler<Command.CreateTenant, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public CreateTenantCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.CreateTenant request, CancellationToken cancellationToken)
        {
            if (!await _access.CanCreateAsync("Tenant", null, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            var entity = new Entity
            {
            };
            entity.TenantId = entity.Id;
            TenantMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, null, entity.Code, null, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            _repository.Add(entity);
            return Result.Success(TenantMapping.ToResponse(entity));
        }
    }
}
