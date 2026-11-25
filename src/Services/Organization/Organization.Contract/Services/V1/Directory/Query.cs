using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Directory
{
    public static class Query
    {
        public sealed record GetTenantSnapshot(Guid TenantId) : IQuery<Response.Snapshot>;
        public sealed record ListDirectoryTenants : IQuery<IReadOnlyList<Response.TenantScope>>;
    }
}
