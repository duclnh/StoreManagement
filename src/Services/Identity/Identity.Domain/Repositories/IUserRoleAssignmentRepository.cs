using Identity.Domain.Entities.Identity;

namespace Identity.Domain.Repositories
{
    public interface IUserRoleAssignmentRepository
    {
        Task<IReadOnlyList<Guid>> GetRoleIdsAtScopeAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<Guid>> GetAllRoleIdsAsync(
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken);

        Task<bool> ReplaceRoleIdsAtScopeAsync(
            Guid userId,
            Guid tenantId,
            Guid scopeId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken);
    }
}
