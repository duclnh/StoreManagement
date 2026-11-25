using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Organizations;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Application.UserCases.V1.Commands.Organizations
{
    public sealed class CreateOrganizationCommandHandler : ICommandHandler<Command.CreateOrganization, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;
        private readonly IRepository<global::Organization.Domain.Entities.Tenants.Tenant> _parentRepository;

        public CreateOrganizationCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access,
            IRepository<global::Organization.Domain.Entities.Tenants.Tenant> parentRepository)
        {
            _repository = repository;
            _access = access;
            _parentRepository = parentRepository;
        }

        public async Task<Result<Response.Detail>> Handle(Command.CreateOrganization request, CancellationToken cancellationToken)
        {
            if (!await _access.CanCreateAsync("Organization", request.TenantId, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            var parent = await _parentRepository.GetAsync(request.TenantId, cancellationToken);
            if (parent is null || !parent.Status)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            var entity = new Entity
            {
                TenantId = parent.Id,
            };
            OrganizationMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, null, entity.Code, null, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            _repository.Add(entity);
            return Result.Success(OrganizationMapping.ToResponse(entity));
        }
    }
}
