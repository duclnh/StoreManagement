using Identity.Application.Abstractions;
using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.UserCases.V1;
using Identity.Application.UserCases.V1.Commands.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Identity.Persistence;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class GetRefreshTokenQueryHandler : IQueryHandler<Query.RefreshToken, Response.Authenticated>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _http;
        private readonly IDataProtectionProvider _protection;
        private readonly ApplicationDbContext _db;
        private readonly IOrganizationScopeProjection _organizationProjection;

        public GetRefreshTokenQueryHandler(
            IJwtTokenService jwtTokenService,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor http,
            IDataProtectionProvider protection,
            ApplicationDbContext db,
            IOrganizationScopeProjection organizationProjection)
        {
            _jwtTokenService = jwtTokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _http = http;
            _protection = protection;
            _db = db;
            _organizationProjection = organizationProjection;
        }

        public async Task<Result<Response.Authenticated>> Handle(Query.RefreshToken request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return InvalidRefreshToken();
            if (!user.IsActive || !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user))
            {
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
                return InvalidRefreshToken();
            }

            var session = await _unitOfWork.LoginSessionRepository.FindByIdAsync(request.SessionId, cancellationToken);
            try
            {
                if (!await _organizationProjection.RefreshAsync(user.TenantId, cancellationToken)
                    && !await _userManager.IsInRoleAsync(user, "Administrator"))
                {
                    return Result.Failure<Response.Authenticated>(new Error("Identity.Tenant.Inactive", "Tenant is unavailable."));
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
            {
                return Result.Failure<Response.Authenticated>(new Error("Identity.Organization.Unavailable", "Organization directory is unavailable."));
            }

            var now = DateTimeOffset.UtcNow;
            if (session is null
                || session.UserId != user.Id
                || session.IsRevoked
                || session.ExpiresAtUtc <= now)
                return InvalidRefreshToken();

            if (string.IsNullOrWhiteSpace(request.Token)) return InvalidRefreshToken();
            var suppliedHash = GetLoginQueryHandler.HashToken(request.Token);
            var protector = _protection.CreateProtector("Identity.RefreshRetry", session.Id.ToString());
            string? refreshToken = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                now = DateTimeOffset.UtcNow;
                if (session.IsRevoked || session.ExpiresAtUtc <= now) return InvalidRefreshToken();
                if (session.CanRetryRefresh(suppliedHash, now))
                {
                    try { refreshToken = protector.Unprotect(session.ProtectedRefreshToken!); }
                    catch (CryptographicException) { return InvalidRefreshToken(); }
                    if (!LoginSession.HashEquals(session.RefreshTokenHash, GetLoginQueryHandler.HashToken(refreshToken)))
                        return InvalidRefreshToken();
                    break;
                }
                if (!LoginSession.HashEquals(session.RefreshTokenHash, suppliedHash)) return InvalidRefreshToken();
                refreshToken = _jwtTokenService.GenerateRefreshToken();
                session.Rotate(GetLoginQueryHandler.HashToken(refreshToken), now.AddDays(30), now, protector.Protect(refreshToken));
                session.RecordActivity(_http.HttpContext?.Connection.RemoteIpAddress?.ToString(), now);
                _unitOfWork.LoginSessionRepository.Update(session);
                try
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    refreshToken = null;
                    await _db.Entry(session).ReloadAsync(cancellationToken);
                }
            }
            if (refreshToken is null) return InvalidRefreshToken();
            var refreshTokenExpiry = session.ExpiresAtUtc;

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Sid, request.SessionId.ToString()),
                new(IdentityClaimTypes.TenantId, user.TenantId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(await IdentityClaimsFactory.GetCustomClaimsAsync(
                user,
                roles,
                _userManager,
                _roleManager));

            var response = new Response.Authenticated
            {
                UserId = user.Id,
                SessionId = request.SessionId,
                AccessToken = _jwtTokenService.GenerateAccessToken(claims),
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiry.UtcDateTime
            };

            return Result.Success(response);
        }

        private static Result<Response.Authenticated> InvalidRefreshToken() =>
            Result.Failure<Response.Authenticated>(new Error("Identity.Authentication.InvalidRefreshToken", "The refresh token is invalid or expired."));
    }
}
