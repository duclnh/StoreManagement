using Identity.Domain.Entities.Identity;

namespace Identity.Domain.Repositories
{
    public sealed record SecurityAuditFilter(
        DateTimeOffset? FromUtc,
        DateTimeOffset? ToUtc,
        string? EventType,
        string? Outcome,
        Guid? UserId,
        int Page,
        int PageSize);

    public sealed record SecurityAuditPage(
        IReadOnlyList<SecurityAuditEvent> Items,
        int Page,
        int PageSize,
        int TotalCount);

    public interface ISecurityAuditRepository
    {
        void Record(
            string eventType,
            string outcome,
            Guid? subjectUserId = null,
            string? details = null,
            Guid? sessionId = null,
            Guid? tenantId = null);

        Task<SecurityAuditPage> GetPageAsync(SecurityAuditFilter filter, CancellationToken cancellationToken);
    }
}
