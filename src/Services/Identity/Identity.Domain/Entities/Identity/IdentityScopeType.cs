namespace Identity.Domain.Entities.Identity
{
    public enum IdentityScopeType
    {
        Tenant = 1,
        Organization = 2,
        Branch = 3
    }

    public static class TenantDefaults
    {
        public static readonly Guid LegacyTenantId = new("11111111-1111-1111-1111-111111111111");
    }
}
