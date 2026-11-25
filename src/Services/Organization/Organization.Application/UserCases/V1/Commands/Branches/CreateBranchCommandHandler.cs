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
    public sealed class CreateBranchCommandHandler : ICommandHandler<Command.CreateBranch, Response.Detail>
    {
        private readonly IRepository<Entity> _repository;
        private readonly IScopeAccess _access;
        private readonly IIdentityDirectory _directory;
        private readonly ITenantContext _context;
        private readonly IRepository<global::Organization.Domain.Entities.Organizations.Organization> _parentRepository;

        public CreateBranchCommandHandler(
            IRepository<Entity> repository,
            IScopeAccess access,
            IIdentityDirectory directory,
            ITenantContext context,
            IRepository<global::Organization.Domain.Entities.Organizations.Organization> parentRepository)
        {
            _repository = repository;
            _access = access;
            _directory = directory;
            _context = context;
            _parentRepository = parentRepository;
        }

        public async Task<Result<Response.Detail>> Handle(Command.CreateBranch request, CancellationToken cancellationToken)
        {
            if (!await _access.CanCreateAsync("Branch", request.OrganizationId, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            var parent = await _parentRepository.GetAsync(request.OrganizationId, cancellationToken);
            if (parent is null || !parent.Status)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            var entity = new Entity
            {
                TenantId = parent.TenantId,
                OrganizationId = parent.Id,
            };
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
            if (await _repository.CodeExistsAsync(entity.TenantId, entity.OrganizationId, entity.Code, null, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            if (entity.IsDefault)
            {
                await _repository.ClearDefaultAsync(entity.TenantId, entity.OrganizationId, entity.Id, cancellationToken);
            }

            _repository.Add(entity);
            return Result.Success(BranchMapping.ToResponse(entity));
        }
    }
}
