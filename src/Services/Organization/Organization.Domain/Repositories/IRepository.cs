using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Entities.Members;

namespace Organization.Domain.Repositories
{
    public interface IRepository<T> where T : ScopedEntity
    {
        Task<T?> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<T>> ListAsync(Guid? parentId, string? search, int page, int pageSize, CancellationToken cancellationToken);
        Task<int> CountAsync(Guid? parentId, string? search, CancellationToken cancellationToken);
        Task<bool> CodeExistsAsync(Guid tenantId, Guid? parentId, string code, Guid? excludingId, CancellationToken cancellationToken);
        Task ClearDefaultAsync(Guid tenantId, Guid parentId, Guid excludingId, CancellationToken cancellationToken);
        void Add(T entity);
    }
}
