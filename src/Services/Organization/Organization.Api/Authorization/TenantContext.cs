using System.Security.Claims;
using Organization.Domain.Abstractions;

namespace Organization.Api.Authorization
{
    public sealed class TenantContext : ITenantContext, IOrganizationContext
    {
        private readonly IHttpContextAccessor _accessor;

        public TenantContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;
        private Guid? Claim(string type) => Guid.TryParse(User?.FindFirstValue(type), out var value) ? value : null;
        public bool IsSystemAdministrator => User?.IsInRole("Administrator") == true;
        public bool IsTenantAdministrator => User?.IsInRole("TenantAdmin") == true || User?.IsInRole("TenantAdministrator") == true;
        public bool IsBranchManager => User?.IsInRole("BranchManager") == true;
        public Guid? UserId => Claim(ClaimTypes.NameIdentifier) ?? Claim("sub");
        public Guid? TenantId => Claim("tenant_id");
        public Guid? OrganizationId => Claim("organization_id");
        public Guid? BranchId => Claim("branch_id");
        public bool HasPermission(string permission) => User?.Claims.Any(x => (x.Type == "permission" || x.Type == "permissions") && x.Value == permission) == true;
    }
}
