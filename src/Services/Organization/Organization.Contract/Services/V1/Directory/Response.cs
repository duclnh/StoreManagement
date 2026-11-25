namespace Organization.Contract.Services.V1.Directory
{
    public static class Response
    {
        public sealed record TenantScope(Guid Id, string Code, string Name, bool Status, DateTimeOffset CreatedAt);
        public sealed record OrganizationScope(Guid Id, Guid TenantId, string Name, bool Status);
        public sealed record BranchScope(Guid Id, Guid OrganizationId, string Name, bool Status);
        public sealed record Snapshot(
            TenantScope Tenant,
            IReadOnlyList<OrganizationScope> Organizations,
            IReadOnlyList<BranchScope> Branches);
    }
}
