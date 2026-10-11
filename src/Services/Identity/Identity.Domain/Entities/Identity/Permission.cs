namespace Identity.Domain.Entities.Identity
{
    public class Permission
    { 
        public Guid RoleId { get; set; }
        public string FunctionId { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty;
    }
}

