namespace Organization.Contract.Services.V1.Organizations
{
    public static class Response
    {
        public sealed record Detail(
            Guid Id,
            Guid TenantId,
            string Code,
            string Name,
            string? TaxCode,
            string BusinessType,
            string? Industry,
            string? Address,
            string? Phone,
            string? Email,
            string? LogoUrl,
            string Currency,
            string TimeZone,
            string Language,
            bool Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}
