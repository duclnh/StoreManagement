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
    public sealed class SetOrganizationStatusCommandHandler : ICommandHandler<Command.SetOrganizationStatus, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public SetOrganizationStatusCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.SetOrganizationStatus request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("Organization", entity.Id, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            entity.Status = request.Status;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(OrganizationMapping.ToResponse(entity));
        }
    }
}
