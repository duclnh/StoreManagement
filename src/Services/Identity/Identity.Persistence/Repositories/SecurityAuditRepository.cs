using System.Security.Claims;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Identity.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories
{
    public sealed class SecurityAuditRepository : ISecurityAuditRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityTenantContext _tenantContext;

        public SecurityAuditRepository(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IIdentityTenantContext tenantContext)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _tenantContext = tenantContext;
        }

        public void Record(
            string eventType,
            string outcome,
            Guid? subjectUserId = null,
            string? details = null,
            Guid? sessionId = null,
            Guid? tenantId = null)
        {
            var context = _httpContextAccessor.HttpContext;
            var principal = context?.User;
            var actorUserId = Guid.TryParse(
                principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                out var parsedActorId)
                ? parsedActorId
                : (Guid?)null;

            if (sessionId is null && Guid.TryParse(principal?.FindFirstValue(ClaimTypes.Sid), out var parsedSessionId))
                sessionId = parsedSessionId;

            var ipAddress = context?.Connection.RemoteIpAddress?.ToString();
            _dbContext.Set<SecurityAuditEvent>().Add(new SecurityAuditEvent(
                eventType,
                outcome,
                DateTimeOffset.UtcNow,
                actorUserId,
                subjectUserId,
                sessionId,
                tenantId ?? _tenantContext.TenantId,
                ipAddress,
                details,
                _tenantContext.RequestedScopeId ?? tenantId));
        }

        public async Task<string?> GetLatestSuccessfulLoginIpAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            await _dbContext.Set<SecurityAuditEvent>()
                .AsNoTracking()
                .Where(item => item.SubjectUserId == userId
                    && item.EventType == SecurityAuditEventTypes.Login
                    && item.Outcome == SecurityAuditOutcomes.Succeeded
                    && item.IpAddress != null)
                .OrderByDescending(item => item.OccurredAtUtc)
                .Select(item => item.IpAddress)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<SecurityAuditPageData> GetPageAsync(
            SecurityAuditFilter filter,
            CancellationToken cancellationToken)
        {
            var query = _dbContext.Set<SecurityAuditEvent>().AsNoTracking();
            if (!_tenantContext.TenantId.HasValue)
                return new SecurityAuditPageData([], filter.Page, filter.PageSize, 0);
            var tenantId = _tenantContext.TenantId.Value;
            query = query.Where(item => item.TenantId == tenantId
                || (_tenantContext.IsPlatformAdministrator && item.TenantId == null));
            if (_tenantContext.RequestedScopeId is Guid scopeId && scopeId != tenantId)
            {
                var branches = _dbContext.Branches.Where(branch => branch.OrganizationId == scopeId
                    && branch.Organization.TenantId == tenantId);
                query = query.Where(item => item.ScopeId == scopeId || branches.Any(branch => branch.Id == item.ScopeId));
            }
            if (filter.FromUtc.HasValue)
                query = query.Where(item => item.OccurredAtUtc >= filter.FromUtc.Value);
            if (filter.ToUtc.HasValue)
                query = query.Where(item => item.OccurredAtUtc < filter.ToUtc.Value);
            if (!string.IsNullOrWhiteSpace(filter.EventType))
                query = query.Where(item => item.EventType == filter.EventType);
            if (!string.IsNullOrWhiteSpace(filter.Outcome))
                query = query.Where(item => item.Outcome == filter.Outcome);
            if (filter.UserId.HasValue)
                query = query.Where(item => item.ActorUserId == filter.UserId || item.SubjectUserId == filter.UserId);
            if (!string.IsNullOrWhiteSpace(filter.UserName))
            {
                var matchingUserIds = _dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.TenantId == _tenantContext.TenantId.Value
                        && ((user.UserName != null && user.UserName.Contains(filter.UserName))
                            || (user.Email != null && user.Email.Contains(filter.UserName))))
                    .Select(user => user.Id);
                query = query.Where(item =>
                    (item.ActorUserId.HasValue && matchingUserIds.Contains(item.ActorUserId.Value))
                    || (item.SubjectUserId.HasValue && matchingUserIds.Contains(item.SubjectUserId.Value)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(item => item.OccurredAtUtc)
                .ThenByDescending(item => item.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new SecurityAuditPageData(items, filter.Page, filter.PageSize, totalCount);
        }

        public async Task<IReadOnlyDictionary<Guid, string?>> GetUserNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
                return new Dictionary<Guid, string?>();

            var users = await _dbContext.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.UserName })
                .ToListAsync(cancellationToken);

            return users.ToDictionary(user => user.Id, user => user.UserName);
        }
    }
}
