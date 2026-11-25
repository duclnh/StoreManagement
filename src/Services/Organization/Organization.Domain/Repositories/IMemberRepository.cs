using Organization.Domain.Entities.Members;

namespace Organization.Domain.Repositories
{
    public interface IMemberRepository
    {
        Task<IReadOnlyList<OrganizationMember>> ListAsync(Guid tenantId, Guid organizationId, Guid? branchId, int page, int pageSize, CancellationToken cancellationToken);
        Task<int> CountAsync(Guid tenantId, Guid organizationId, Guid? branchId, CancellationToken cancellationToken);
        Task<OrganizationMember?> FindAsync(Guid tenantId, Guid organizationId, Guid userId, Guid? branchId, CancellationToken cancellationToken);
        Task<bool> HasAccessAsync(Guid tenantId, Guid organizationId, Guid? branchId, Guid userId, CancellationToken cancellationToken);
        Task ClearDefaultAsync(Guid tenantId, Guid userId, Guid excludingId, CancellationToken cancellationToken);
        Task<int> RemoveOrganizationAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken cancellationToken);
        void Add(OrganizationMember member);
        void Remove(OrganizationMember member);
    }
}
