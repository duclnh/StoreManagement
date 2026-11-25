using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Tenants
{
    public static class Command
    {
        public sealed record Data(
            string Code,
            string Name,
            string? ContactEmail,
            string? ContactPhone,
            string ConfigurationJson,
            bool Status = true);

        public sealed record CreateTenant(Data Data) : ICommand<Response.Detail>;
        public sealed record UpdateTenant(Guid Id, Data Data) : ICommand<Response.Detail>;
        public sealed record SetTenantStatus(Guid Id, bool Status) : ICommand<Response.Detail>;
    }
}
