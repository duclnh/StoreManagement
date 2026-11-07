namespace Identity.Domain.Abstractions
{
    public interface IIdentityTenantContext
    {
        Guid? TenantId { get; }
        Guid? UserId { get; }
        Guid? RequestedScopeId { get; }
        bool IsPlatformAdministrator { get; }
    }

    public static class IdentityClaimTypes
    {
        public const string TenantId = "tenant_id";
    }
}
