namespace Identity.Domain.Repositories
{
    public enum IdentityScopeMutationStatus
    {
        Succeeded,
        NotFound,
        Conflict
    }

    public sealed record IdentityScopeMutationResult(
        IdentityScopeMutationStatus Status,
        Guid? ScopeId = null);

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
        Task<IdentityScopeMutationResult> CreateTenantAsync(string name, CancellationToken cancellationToken);
        Task<IdentityScopeMutationResult> CreateOrganizationAsync(
            Guid tenantId,
            string name,
            CancellationToken cancellationToken);
        Task<IdentityScopeMutationResult> CreateBranchAsync(
            Guid tenantId,
            Guid organizationId,
            string name,
            CancellationToken cancellationToken);
        Task<IdentityScopeMutationStatus> SetTenantActiveAsync(
            Guid tenantId,
            bool isActive,
            CancellationToken cancellationToken);
        Task<IdentityScopeMutationStatus> SetOrganizationActiveAsync(
            Guid tenantId,
            Guid organizationId,
            bool isActive,
            CancellationToken cancellationToken);
        Task<IdentityScopeMutationStatus> SetBranchActiveAsync(
            Guid tenantId,
            Guid organizationId,
            Guid branchId,
            bool isActive,
            CancellationToken cancellationToken);
    }
}
