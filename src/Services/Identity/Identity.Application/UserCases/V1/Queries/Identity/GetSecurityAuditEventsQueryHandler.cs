using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public sealed class GetSecurityAuditEventsQueryHandler
        : IQueryHandler<GetSecurityAuditEvents, PageResult<Response.SecurityAuditEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityTenantContext _tenantContext;

        public GetSecurityAuditEventsQueryHandler(
            IUnitOfWork unitOfWork,
            IIdentityTenantContext tenantContext)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<Result<PageResult<Response.SecurityAuditEvent>>> Handle(
            GetSecurityAuditEvents request,
            CancellationToken cancellationToken)
        {
            if (!_tenantContext.TenantId.HasValue)
                return Result.Failure<PageResult<Response.SecurityAuditEvent>>(new Error(
                    "Identity.SecurityAudit.TenantRequired",
                    "A tenant context is required to view security audit events."));

            if (request.Page < 1
                || request.PageSize is < 1 or > 100
                || request.Page > int.MaxValue / request.PageSize)
                return Result.Failure<PageResult<Response.SecurityAuditEvent>>(new Error(
                    "Identity.SecurityAudit.InvalidPage",
                    "Page must be positive and page size must be between 1 and 100."));

            if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.FromUtc >= request.ToUtc)
                return Result.Failure<PageResult<Response.SecurityAuditEvent>>(new Error(
                    "Identity.SecurityAudit.InvalidRange",
                    "The audit range start must be earlier than its end."));

            if (request.EventType?.Length > 100
                || request.UserName?.Length > 256
                || (request.Outcome is not null
                    && request.Outcome is not SecurityAuditOutcomes.Succeeded and not SecurityAuditOutcomes.Failed))
            {
                return Result.Failure<PageResult<Response.SecurityAuditEvent>>(new Error(
                    "Identity.SecurityAudit.InvalidFilter",
                    "The audit event type, outcome, or username filter is invalid."));
            }

            var page = await _unitOfWork.SecurityAuditRepository.GetPageAsync(
                new SecurityAuditFilter(
                    request.FromUtc,
                    request.ToUtc,
                    request.EventType,
                    request.Outcome,
                    request.UserId,
                    request.UserName?.Trim(),
                    request.Page,
                    request.PageSize),
                cancellationToken);
            var userIds = page.Items
                .SelectMany(item => new[] { item.ActorUserId, item.SubjectUserId })
                .OfType<Guid>()
                .Distinct()
                .ToArray();
            var userNames = await _unitOfWork.SecurityAuditRepository.GetUserNamesAsync(
                userIds,
                cancellationToken);

            var items = page.Items.Select(item => new Response.SecurityAuditEvent(
                    item.Id,
                    item.EventType,
                    item.Outcome,
                    item.OccurredAtUtc,
                    item.ActorUserId,
                    item.ActorUserId.HasValue && userNames.TryGetValue(item.ActorUserId.Value, out var actorUserName)
                        ? actorUserName
                        : null,
                    item.SubjectUserId,
                    item.SubjectUserId.HasValue && userNames.TryGetValue(item.SubjectUserId.Value, out var subjectUserName)
                        ? subjectUserName
                        : null,
                    item.SessionId,
                    item.IpAddress,
                    item.Details))
                .ToList();
            return Result.Success(PageResult<Response.SecurityAuditEvent>.Create(
                items,
                page.Page,
                page.PageSize,
                page.TotalCount));
        }
    }
}
