using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Enumerations;

namespace Organization.Domain.Entities.Tenants
{
    public sealed class Tenant : ScopedEntity
    {
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string ConfigurationJson { get; set; } = "{}";
    }
}
