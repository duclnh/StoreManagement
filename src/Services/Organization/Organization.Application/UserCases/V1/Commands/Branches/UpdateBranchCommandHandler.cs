using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Application.Mapping;
using Organization.Contract.Services.V1.Branches;
using Organization.Contract.Shared;
using Organization.Domain.Repositories;
using Organization.Domain.Abstractions;
using Entity = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Application.UserCases.V1.Commands.Branches
{
    public sealed class UpdateBranchCommandHandler : ICommandHandler<Command.UpdateBranch, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;
        private readonly IIdentityDirectory _directory;
        private readonly ITenantContext _context;

        public UpdateBranchCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access,
            IIdentityDirectory directory,
            ITenantContext context)
        {
            _repository = repository;
            _access = access;
            _directory = directory;
            _context = context;
        }

        public async Task<Result<Response.Detail>> Handle(Command.UpdateBranch request, CancellationToken cancellationToken)
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

            if (request.Data.ManagerUserId is Guid managerId
                && !await _directory.UserExistsAsync(managerId, entity.TenantId, cancellationToken))
            {
                return Result.Failure<Response.Detail>(new Error("Organization.User.Invalid", "Manager must be an active user in this tenant."));
            }

            if (request.Data.IsDefault != entity.IsDefault && !_context.IsSystemAdministrator && !_context.IsTenantAdministrator)
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            BranchMapping.Apply(entity, request.Data);
            if (await _repository.CodeExistsAsync(entity.TenantId, entity.OrganizationId, entity.Code, entity.Id, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            if (entity.IsDefault)
            {
                await _repository.ClearDefaultAsync(entity.TenantId, entity.OrganizationId, entity.Id, cancellationToken);
            }

            return Result.Success(BranchMapping.ToResponse(entity));
        }
    }
}
