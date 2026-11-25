namespace Identity.Domain.Abstractions
{
    public sealed record TenantScopeSnapshot(Guid Id, string Code, string Name, bool Status, DateTimeOffset CreatedAt);
    public sealed record OrganizationScopeSnapshot(Guid Id, Guid TenantId, string Name, bool Status);
    public sealed record BranchScopeSnapshot(Guid Id, Guid OrganizationId, string Name, bool Status);
    public sealed record OrganizationScopeSnapshotSet(
        TenantScopeSnapshot Tenant,
        IReadOnlyList<OrganizationScopeSnapshot> Organizations,
        IReadOnlyList<BranchScopeSnapshot> Branches);

    public interface IOrganizationScopeDirectory
    {
        Task<IReadOnlyList<TenantScopeSnapshot>> GetTenantsAsync(CancellationToken cancellationToken);
        Task<OrganizationScopeSnapshotSet?> GetAsync(Guid tenantId, CancellationToken cancellationToken);
    }

    public interface IOrganizationScopeProjection
    {
        Task<bool> RefreshAsync(Guid tenantId, CancellationToken cancellationToken);
    }
}
