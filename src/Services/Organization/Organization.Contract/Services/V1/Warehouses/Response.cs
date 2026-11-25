namespace Organization.Contract.Services.V1.Warehouses
{
    public static class Response
    {
        public sealed record Detail(
            Guid Id,
            Guid TenantId,
            Guid BranchId,
            string Code,
            string Name,
            string WarehouseType,
            string? Address,
            bool IsDefault,
            bool Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}
