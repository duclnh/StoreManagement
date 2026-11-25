using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Contract.Services.V1.Members;
using Organization.Contract.Shared;
using Organization.Domain.Entities.Members;
using Organization.Domain.Repositories;
using Parent = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Application.UserCases.V1.Queries.Members
{
    public sealed class ListBranchMembersQueryHandler : IQueryHandler<Query.ListBranchMembers, Page<Response.Detail>>
    {
        private readonly IRepository<Parent> _repository;
        private readonly IMemberRepository _members;
        private readonly IScopeAccess _access;

        public ListBranchMembersQueryHandler(IRepository<Parent> repository, IMemberRepository members, IScopeAccess access)
        {
            _repository = repository;
            _members = members;
            _access = access;
        }

        public async Task<Result<Page<Response.Detail>>> Handle(Query.ListBranchMembers request, CancellationToken cancellationToken)
        {
            if (!await _access.CanAccessAsync("Branch", request.BranchId, false, cancellationToken))
            {
                return Result.Failure<Page<Response.Detail>>(Errors.Forbidden);
            }

            var parent = await _repository.GetAsync(request.BranchId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Page<Response.Detail>>(Errors.NotFound);
            }

            var organizationId = parent.OrganizationId;
            Guid? branchId = parent.Id;
            var members = await _members.ListAsync(parent.TenantId, organizationId, branchId, request.Page, request.PageSize, cancellationToken);
            var count = await _members.CountAsync(parent.TenantId, organizationId, branchId, cancellationToken);
            return Result.Success(new Page<Response.Detail>(members.Select(x => new Response.Detail(x.Id, x.OrganizationId, x.UserId, x.BranchId, x.IsDefault, x.Status)).ToArray(), request.Page, request.PageSize, count));
        }
    }
}
