namespace Organization.Contract.Services.V1.PointOfSales
{
    public static class Response
    {
        public sealed record Detail(
            Guid Id,
            Guid TenantId,
            Guid BranchId,
            string Code,
            string Name,
            string? Description,
            bool Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}
