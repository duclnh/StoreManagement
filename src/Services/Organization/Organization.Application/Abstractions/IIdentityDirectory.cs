namespace Organization.Application.Abstractions
{
    public interface IIdentityDirectory
    {
        Task<bool> UserExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    }
}
