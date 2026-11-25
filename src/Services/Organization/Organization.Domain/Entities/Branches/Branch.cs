using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Enumerations;

namespace Organization.Domain.Entities.Branches
{
    public sealed class Branch : ScopedEntity
    {
        public Guid OrganizationId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public Guid? ManagerUserId { get; set; }
        public bool IsDefault { get; set; }
    }
}
