using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Organizations
{
    public static class Command
    {
        public sealed record Data(
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
            bool Status = true);

        public sealed record CreateOrganization(Guid TenantId, Data Data) : ICommand<Response.Detail>;
        public sealed record UpdateOrganization(Guid Id, Data Data) : ICommand<Response.Detail>;
        public sealed record SetOrganizationStatus(Guid Id, bool Status) : ICommand<Response.Detail>;
    }
}
