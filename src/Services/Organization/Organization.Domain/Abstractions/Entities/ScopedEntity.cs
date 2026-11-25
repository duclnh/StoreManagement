namespace Organization.Domain.Abstractions.Entities
{
    public abstract class ScopedEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public void Rename(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 32
                || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150)
            {
                throw new ArgumentException("Code and name are invalid.");
            }

            Code = code.Trim().ToUpperInvariant();
            Name = name.Trim();
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
