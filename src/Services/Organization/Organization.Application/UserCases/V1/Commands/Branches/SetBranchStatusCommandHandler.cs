using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Branches;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Entity = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Application.UserCases.V1.Commands.Branches
{
    public sealed class SetBranchStatusCommandHandler : ICommandHandler<Command.SetBranchStatus, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;

        public SetBranchStatusCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access)
        {
            _repository = repository;
            _access = access;
        }

        public async Task<Result<Response.Detail>> Handle(Command.SetBranchStatus request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            if (!await _access.CanAccessAsync("Branch", entity.Id, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            entity.Status = request.Status;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(BranchMapping.ToResponse(entity));
        }
    }
}
