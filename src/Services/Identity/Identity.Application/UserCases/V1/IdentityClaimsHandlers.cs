using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using static Identity.Contract.Services.V1.Identity.Command;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1
{
    internal static class IdentityClaimsPolicy
    {
        private static readonly HashSet<string> SystemClaimTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ClaimTypes.Name,
            ClaimTypes.NameIdentifier,
            ClaimTypes.Email,
            ClaimTypes.Role,
            ClaimTypes.Sid,
            IdentityClaimTypes.TenantId,
            "sub",
            "email",
            "role",
            "tenantId"
        };

        public static string? Validate(string? type, string? value)
        {
            if (string.IsNullOrWhiteSpace(type) || type.Length > 256)
                return "Claim type is required and must be at most 256 characters.";
            if (value is null || value.Length > 1024)
                return "Claim value is required and must be at most 1024 characters.";
            if (SystemClaimTypes.Contains(type.Trim()))
                return "System identity claim types cannot be managed as custom claims.";
            return null;
        }

        public static bool IsSystemClaim(string type) => SystemClaimTypes.Contains(type);
    }

    internal static class IdentityClaimsFactory
    {
        public static async Task<IReadOnlyList<Claim>> GetCustomClaimsAsync(
            AppUser user,
            IEnumerable<string> roleNames,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            var claims = (await userManager.GetClaimsAsync(user)).ToList();
            foreach (var roleName in roleNames)
            {
                var role = await roleManager.FindByNameAsync(roleName)
                    ?? throw new InvalidOperationException($"Role '{roleName}' could not be resolved while creating authentication claims.");
                claims.AddRange(await roleManager.GetClaimsAsync(role));
            }

            return claims
                .Where(claim => !IdentityClaimsPolicy.IsSystemClaim(claim.Type))
                .DistinctBy(claim => (claim.Type, claim.Value))
                .ToArray();
        }
    }

    public sealed class GetIdentityClaimsQueryHandler
        : IQueryHandler<GetIdentityClaims, List<Response.IdentityClaim>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IIdentityTenantContext _tenantContext;

        public GetIdentityClaimsQueryHandler(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IIdentityTenantContext tenantContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantContext = tenantContext;
        }

        public async Task<Result<List<Response.IdentityClaim>>> Handle(
            GetIdentityClaims request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId != request.TenantId)
                return Result.Failure<List<Response.IdentityClaim>>(new Error(
                    "Identity.Tenant.Invalid",
                    "The requested tenant does not match the authenticated tenant."));

            IList<Claim> claims;
            if (request.OwnerType == IdentityClaimOwnerType.User)
            {
                var user = await _userManager.FindByIdAsync(request.OwnerId.ToString());
                if (user is null || user.TenantId != request.TenantId)
                    return Result.Failure<List<Response.IdentityClaim>>(new Error(
                        "Identity.User.NotFound",
                        "The user was not found."));
                claims = await _userManager.GetClaimsAsync(user);
            }
            else
            {
                var role = await _roleManager.FindByIdAsync(request.OwnerId.ToString());
                if (role is null || role.TenantId != request.TenantId)
                    return Result.Failure<List<Response.IdentityClaim>>(new Error(
                        "Identity.Role.NotFound",
                        "The role was not found."));
                claims = await _roleManager.GetClaimsAsync(role);
            }

            return Result.Success(claims
                .Where(claim => !IdentityClaimsPolicy.IsSystemClaim(claim.Type))
                .Select(claim => new Response.IdentityClaim(claim.Type, claim.Value))
                .OrderBy(claim => claim.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(claim => claim.Value, StringComparer.Ordinal)
                .ToList());
        }
    }

    public sealed class ChangeIdentityClaimCommandHandler
        : ICommandHandler<ChangeIdentityClaim, bool>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IIdentityTenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeIdentityClaimCommandHandler(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IIdentityTenantContext tenantContext,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            ChangeIdentityClaim request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext.TenantId != request.TenantId)
                return Result.Failure<bool>(new Error(
                    "Identity.Tenant.Invalid",
                    "The requested tenant does not match the authenticated tenant."));

            var validationError = IdentityClaimsPolicy.Validate(request.Type, request.Value);
            if (validationError is not null)
                return Result.Failure<bool>(new Error("Identity.Claim.Invalid", validationError));

            var type = request.Type.Trim();
            var claim = new Claim(type, request.Value);
            IdentityResult result;
            Guid? subjectUserId = null;
            string eventType;
            string subject;

            if (request.OwnerType == IdentityClaimOwnerType.User)
            {
                var user = await _userManager.FindByIdAsync(request.OwnerId.ToString());
                if (user is null || user.TenantId != request.TenantId)
                    return Result.Failure<bool>(new Error("Identity.User.NotFound", "The user was not found."));

                subjectUserId = user.Id;
                var existing = (await _userManager.GetClaimsAsync(user))
                    .Where(existingClaim => existingClaim.Type == type && existingClaim.Value == request.Value)
                    .ToArray();
                if (request.Add)
                {
                    if (existing.Length == 0)
                    {
                        result = await _userManager.AddClaimAsync(user, claim);
                        if (!result.Succeeded)
                            return Failed(result);
                        eventType = SecurityAuditEventTypes.UserClaimsChanged;
                        subject = "Added";
                    }
                    else
                    {
                        return Result.Success(true);
                    }
                }
                else
                {
                    if (existing.Length == 0)
                        return Result.Success(true);
                    result = await _userManager.RemoveClaimsAsync(user, existing);
                    if (!result.Succeeded)
                        return Failed(result);
                    eventType = SecurityAuditEventTypes.UserClaimsChanged;
                    subject = "Removed";
                }
            }
            else
            {
                var role = await _roleManager.FindByIdAsync(request.OwnerId.ToString());
                if (role is null || role.TenantId != request.TenantId)
                    return Result.Failure<bool>(new Error("Identity.Role.NotFound", "The role was not found."));

                var existing = (await _roleManager.GetClaimsAsync(role))
                    .Where(existingClaim => existingClaim.Type == type && existingClaim.Value == request.Value)
                    .ToArray();
                if (request.Add)
                {
                    if (existing.Length == 0)
                    {
                        result = await _roleManager.AddClaimAsync(role, claim);
                        if (!result.Succeeded)
                            return Failed(result);
                        eventType = SecurityAuditEventTypes.RoleClaimsChanged;
                        subject = "Added";
                    }
                    else
                    {
                        return Result.Success(true);
                    }
                }
                else
                {
                    if (existing.Length == 0)
                        return Result.Success(true);
                    foreach (var existingClaim in existing)
                    {
                        result = await _roleManager.RemoveClaimAsync(role, existingClaim);
                        if (!result.Succeeded)
                            return Failed(result);
                    }
                    eventType = SecurityAuditEventTypes.RoleClaimsChanged;
                    subject = "Removed";
                }
            }

            _unitOfWork.SecurityAuditRepository.Record(
                eventType,
                SecurityAuditOutcomes.Succeeded,
                subjectUserId,
                $"Owner:{request.OwnerType};OwnerId:{request.OwnerId};ClaimType:{type};{subject}",
                tenantId: request.TenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Success(true);
        }

        private static Result<bool> Failed(IdentityResult result) =>
            Result.Failure<bool>(new Error(
                "Identity.Claim.UpdateFailed",
                string.Join("; ", result.Errors.Select(error => error.Description))));
    }
}
