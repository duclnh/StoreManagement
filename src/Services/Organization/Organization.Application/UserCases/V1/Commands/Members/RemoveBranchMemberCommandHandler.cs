using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Contract.Services.V1.Members;
using Organization.Contract.Shared;
using Organization.Domain.Entities.Members;
using Organization.Domain.Repositories;
using Parent = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Application.UserCases.V1.Commands.Members
{
    public sealed class RemoveBranchMemberCommandHandler : ICommandHandler<Command.RemoveBranchMember, bool>
    {
        private readonly IRepository<Parent> _repository;
        private readonly IMemberRepository _members;
        private readonly IScopeAccess _access;

        public RemoveBranchMemberCommandHandler(IRepository<Parent> repository, IMemberRepository members, IScopeAccess access)
        {
            _repository = repository;
            _members = members;
            _access = access;
        }

        public async Task<Result<bool>> Handle(Command.RemoveBranchMember request, CancellationToken cancellationToken)
        {
            if (!await _access.CanAccessAsync("Branch", request.BranchId, true, cancellationToken))
            {
                return Result.Failure<bool>(Errors.Forbidden);
            }

            var parent = await _repository.GetAsync(request.BranchId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<bool>(Errors.NotFound);
            }

            var organizationId = parent.OrganizationId;
            Guid? branchId = parent.Id;
            var member = await _members.FindAsync(parent.TenantId, organizationId, request.UserId, branchId, cancellationToken);
            if (member is null)
            {
                return Result.Failure<bool>(Errors.NotFound);
            }

            _members.Remove(member);
            return Result.Success(true);
        }
    }
}
