using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Organization.Application.Abstractions;
using Organization.Contract.Services.V1.Members;
using Organization.Contract.Shared;
using Organization.Domain.Entities.Members;
using Organization.Domain.Repositories;
using Organization.Domain.Abstractions;
using Parent = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Application.UserCases.V1.Queries.Members
{
    public sealed class ListOrganizationMembersQueryHandler : IQueryHandler<Query.ListOrganizationMembers, Page<Response.Detail>>
    {
        private readonly IRepository<Parent> _repository;
        private readonly IMemberRepository _members;
        private readonly IScopeAccess _access;
        private readonly ITenantContext _context;

        public ListOrganizationMembersQueryHandler(IRepository<Parent> repository, IMemberRepository members, IScopeAccess access, ITenantContext context)
        {
            _repository = repository;
            _members = members;
            _access = access;
            _context = context;
        }

        public async Task<Result<Page<Response.Detail>>> Handle(Query.ListOrganizationMembers request, CancellationToken cancellationToken)
        {
            if (!await _access.CanAccessAsync("Organization", request.OrganizationId, false, cancellationToken))
            {
                return Result.Failure<Page<Response.Detail>>(Errors.Forbidden);
            }

            var parent = await _repository.GetAsync(request.OrganizationId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Page<Response.Detail>>(Errors.NotFound);
            }

            var organizationId = parent.Id;
            Guid? branchId = null;
            if (!_context.IsSystemAdministrator && !_context.IsTenantAdministrator
                && (_context.UserId is not Guid userId
                    || !await _members.HasAccessAsync(parent.TenantId, organizationId, null, userId, cancellationToken)))
            {
                return Result.Failure<Page<Response.Detail>>(Errors.Forbidden);
            }

            var members = await _members.ListAsync(parent.TenantId, organizationId, branchId, request.Page, request.PageSize, cancellationToken);
            var count = await _members.CountAsync(parent.TenantId, organizationId, branchId, cancellationToken);
            return Result.Success(new Page<Response.Detail>(members.Select(x => new Response.Detail(x.Id, x.OrganizationId, x.UserId, x.BranchId, x.IsDefault, x.Status)).ToArray(), request.Page, request.PageSize, count));
        }
    }
}
