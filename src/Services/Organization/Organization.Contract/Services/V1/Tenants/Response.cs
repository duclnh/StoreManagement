namespace Organization.Contract.Services.V1.Tenants
{
    public static class Response
    {
        public sealed record Detail(
            Guid Id,
            Guid TenantId,
            string Code,
            string Name,
            string? ContactEmail,
            string? ContactPhone,
            string ConfigurationJson,
            bool Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}
