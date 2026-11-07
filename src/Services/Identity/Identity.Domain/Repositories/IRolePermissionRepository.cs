namespace Identity.Domain.Repositories
{
    public sealed record IdentityScopePath(Guid TenantId, Guid? OrganizationId, Guid? BranchId);
    public sealed record IdentityScopeEntry(Guid Id, string Name, int ScopeType, Guid? ParentId);

    public sealed record RolePermissionEntry(
        string FunctionId,
        string FunctionName,
        string ActionId,
        string ActionName,
        bool IsGranted);

    public interface IRolePermissionRepository
    {
        Task<bool> RoleExistsAsync(Guid roleId, Guid tenantId, CancellationToken cancellationToken);
        Task<IdentityScopePath?> GetScopePathAsync(Guid tenantId, Guid scopeId, CancellationToken cancellationToken);
        Task<IReadOnlyList<IdentityScopeEntry>> GetScopesAsync(Guid tenantId, CancellationToken cancellationToken);
        Task<IReadOnlyList<RolePermissionEntry>> GetRolePermissionsAsync(
            Guid roleId,
            Guid tenantId,
            Guid scopeId,
            CancellationToken cancellationToken);
        Task<bool> PermissionExistsAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            string functionId,
            string actionId,
            CancellationToken cancellationToken);
        Task<bool> ReplaceRolePermissionsAsync(
            Guid roleId,
            Guid tenantId,
            Guid scopeId,
            IReadOnlyCollection<(string FunctionId, string ActionId)> permissions,
            CancellationToken cancellationToken);
    }
}
