namespace Identity.Domain.Entities.Identity
{
    public class Organization
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ICollection<Branch> Branches { get; set; } = [];
    }
}
