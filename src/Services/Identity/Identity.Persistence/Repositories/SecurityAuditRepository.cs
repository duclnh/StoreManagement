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
                details));
        }

        public async Task<SecurityAuditPage> GetPageAsync(
            SecurityAuditFilter filter,
            CancellationToken cancellationToken)
        {
            var query = _dbContext.Set<SecurityAuditEvent>().AsNoTracking();
            if (!_tenantContext.TenantId.HasValue)
                return new SecurityAuditPage([], filter.Page, filter.PageSize, 0);
            query = query.Where(item => item.TenantId == _tenantContext.TenantId.Value);
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

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(item => item.OccurredAtUtc)
                .ThenByDescending(item => item.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new SecurityAuditPage(items, filter.Page, filter.PageSize, totalCount);
        }
    }
}
