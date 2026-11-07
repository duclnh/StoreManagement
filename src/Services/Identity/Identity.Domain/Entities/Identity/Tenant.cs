namespace Identity.Domain.Entities.Identity
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public virtual ICollection<Organization> Organizations { get; set; } = [];
    }
}
