namespace Organization.Application.Abstractions
{
    public interface IScopeAccess
    {
        Task<bool> CanCreateAsync(string resource, Guid? parentId, CancellationToken cancellationToken);
        Task<bool> CanListAsync(string resource, Guid? parentId, CancellationToken cancellationToken);
        Task<bool> CanAccessAsync(string resource, Guid id, bool write, CancellationToken cancellationToken);
    }
}
