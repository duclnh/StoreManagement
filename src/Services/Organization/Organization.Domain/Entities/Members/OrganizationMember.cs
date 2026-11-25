namespace Organization.Domain.Entities.Members
{
    public sealed class OrganizationMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? BranchId { get; set; }
        public bool IsDefault { get; set; }
        public bool Status { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
