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
    public sealed class AddOrganizationMemberCommandHandler : ICommandHandler<Command.AddOrganizationMember, Response.Detail>
    {
        private readonly IRepository<Parent> _repository;
        private readonly IMemberRepository _members;
        private readonly IScopeAccess _access;
        private readonly IIdentityDirectory _directory;

        public AddOrganizationMemberCommandHandler(IRepository<Parent> repository, IMemberRepository members, IScopeAccess access, IIdentityDirectory directory)
        {
            _repository = repository;
            _members = members;
            _access = access;
            _directory = directory;
        }

        public async Task<Result<Response.Detail>> Handle(Command.AddOrganizationMember request, CancellationToken cancellationToken)
        {
            if (!await _access.CanAccessAsync("Organization", request.OrganizationId, true, cancellationToken))
            {
                return Result.Failure<Response.Detail>(Errors.Forbidden);
            }

            var parent = await _repository.GetAsync(request.OrganizationId, cancellationToken);
            if (parent is null || !parent.Status)
            {
                return Result.Failure<Response.Detail>(Errors.NotFound);
            }

            var organizationId = parent.Id;
            Guid? branchId = null;
            if (!await _directory.UserExistsAsync(request.Data.UserId, parent.TenantId, cancellationToken))
            {
                return Result.Failure<Response.Detail>(new Error("Organization.User.Invalid", "The user is not an active user of this tenant."));
            }

            if (await _members.FindAsync(parent.TenantId, organizationId, request.Data.UserId, branchId, cancellationToken) is not null)
            {
                return Result.Failure<Response.Detail>(Errors.Conflict);
            }

            var member = new OrganizationMember
            {
                TenantId = parent.TenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                UserId = request.Data.UserId,
                IsDefault = request.Data.IsDefault,
                Status = request.Data.Status
            };
            if (member.IsDefault)
            {
                await _members.ClearDefaultAsync(parent.TenantId, member.UserId, member.Id, cancellationToken);
            }

            _members.Add(member);
            return Result.Success(new Response.Detail(member.Id, member.OrganizationId, member.UserId, member.BranchId, member.IsDefault, member.Status));
        }
    }
}
