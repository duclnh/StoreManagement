namespace Identity.Domain.Entities.Identity
{
    public class Branch
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public virtual Organization Organization { get; set; } = null!;
    }
}
