namespace Identity.Domain.Entities.Identity
{
    public class UserRoleAssignment
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public IdentityScopeType ScopeType { get; set; }
        public Guid ScopeId { get; set; }
        public virtual AppUser User { get; set; } = null!;
        public virtual AppRole Role { get; set; } = null!;
    }
}
