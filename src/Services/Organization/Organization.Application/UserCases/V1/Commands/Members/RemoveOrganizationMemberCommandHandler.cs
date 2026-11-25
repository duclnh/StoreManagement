using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Contract.Services.V1.Members;
using Organization.Contract.Shared;
using Organization.Domain.Entities.Members;
using Organization.Domain.Repositories;
using Parent = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Application.UserCases.V1.Commands.Members
{
    public sealed class RemoveOrganizationMemberCommandHandler : ICommandHandler<Command.RemoveOrganizationMember, bool>
    {
        private readonly IRepository<Parent> _repository;
        private readonly IMemberRepository _members;
        private readonly IScopeAccess _access;

        public RemoveOrganizationMemberCommandHandler(IRepository<Parent> repository, IMemberRepository members, IScopeAccess access)
        {
            _repository = repository;
            _members = members;
            _access = access;
        }

        public async Task<Result<bool>> Handle(Command.RemoveOrganizationMember request, CancellationToken cancellationToken)
        {
            if (!await _access.CanAccessAsync("Organization", request.OrganizationId, true, cancellationToken))
            {
                return Result.Failure<bool>(Errors.Forbidden);
            }

            var parent = await _repository.GetAsync(request.OrganizationId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<bool>(Errors.NotFound);
            }

            var organizationId = parent.Id;
            var removed = await _members.RemoveOrganizationAsync(parent.TenantId, organizationId, request.UserId, cancellationToken);
            if (removed == 0)
            {
                return Result.Failure<bool>(Errors.NotFound);
            }

            return Result.Success(true);
        }
    }
}
