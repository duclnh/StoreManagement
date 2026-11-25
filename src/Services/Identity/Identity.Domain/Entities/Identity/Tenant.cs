namespace Identity.Domain.Entities.Identity
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset StartsAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndsAtUtc { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string ConfigurationJson { get; set; } = "{}";
        public virtual ICollection<Organization> Organizations { get; set; } = [];
    }
}
