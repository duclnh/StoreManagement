namespace Organization.Domain.Repositories
{
    public sealed record TenantScope(Guid Id, string Code, string Name, bool Status, DateTimeOffset CreatedAt);
    public sealed record OrganizationScope(Guid Id, Guid TenantId, string Name, bool Status);
    public sealed record BranchScope(Guid Id, Guid OrganizationId, string Name, bool Status);
    public sealed record ScopeSnapshot(TenantScope Tenant, IReadOnlyList<OrganizationScope> Organizations, IReadOnlyList<BranchScope> Branches);

    public interface IScopeDirectoryRepository
    {
        Task<IReadOnlyList<TenantScope>> GetTenantsAsync(CancellationToken cancellationToken);
        Task<ScopeSnapshot?> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken);
    }
}
