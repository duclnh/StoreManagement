namespace Identity.Domain.Repositories
{
    public sealed record IdentityTenantEntry(
        Guid Id,
        string Name,
        bool IsActive,
        DateTimeOffset CreatedAtUtc);

    public sealed record IdentityAdminScopeEntry(
        Guid Id,
        string Name,
        int ScopeType,
        Guid? ParentId,
        bool IsActive);

    public interface IIdentityScopeRepository
    {
        Task<IReadOnlyList<IdentityTenantEntry>> GetTenantsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<IdentityAdminScopeEntry>> GetTenantScopesAsync(
            Guid tenantId,
            CancellationToken cancellationToken);
        Task<IdentityScopeEntry?> GetScopeAsync(Guid scopeId, CancellationToken cancellationToken);
    }
}
