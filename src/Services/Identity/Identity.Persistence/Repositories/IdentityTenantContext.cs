using System.Security.Claims;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;

namespace Identity.Persistence.Repositories
{
    public sealed class IdentityTenantContext : IIdentityTenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityTenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? TenantId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                var principal = context?.User;
                if (principal?.IsInRole("Administrator") == true
                    && Guid.TryParse(context?.Request.Headers["X-Tenant-Id"], out var requestedTenantId))
                    return requestedTenantId;

                return Guid.TryParse(principal?.FindFirstValue(IdentityClaimTypes.TenantId), out var tenantId)
                    ? tenantId
                    : null;
            }
        }

        public Guid? UserId =>
            Guid.TryParse(
                _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId)
                ? userId
                : null;

        public Guid? RequestedScopeId
        {
            get
            {
                if (Guid.TryParse(_httpContextAccessor.HttpContext?.Request.Query["scopeId"], out var scopeId))
                    return scopeId;
                return TenantId;
            }
        }

        public bool IsPlatformAdministrator =>
            _httpContextAccessor.HttpContext?.User.IsInRole("Administrator") == true;
    }
}
