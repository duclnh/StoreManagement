namespace Organization.Contract.Services.V1.Branches
{
    public static class Response
    {
        public sealed record Detail(
            Guid Id,
            Guid TenantId,
            Guid OrganizationId,
            string Code,
            string Name,
            string? Address,
            string? Phone,
            string? Email,
            Guid? ManagerUserId,
            bool IsDefault,
            bool Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}
