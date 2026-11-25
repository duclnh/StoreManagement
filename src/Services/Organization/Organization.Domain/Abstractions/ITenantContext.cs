namespace Organization.Domain.Abstractions
{
    public interface ITenantContext
    {
        Guid? TenantId { get; }
        Guid? UserId { get; }
        bool IsSystemAdministrator { get; }
        bool IsTenantAdministrator { get; }
        bool IsBranchManager { get; }
        bool HasPermission(string permission);
    }
}
