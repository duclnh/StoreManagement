using Identity.Application.Abstractions;
using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Identity.Application.UserCases.V1;
using Identity.Application.UserCases.V1.Commands.Identity;
using Identity.Application.DependencyInjection.Options;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Identity.Persistence;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class GetLoginQueryHandler : IQueryHandler<Query.Login, Response.Authenticated>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LoginSessionOptions _sessionOptions;
        private readonly ApplicationDbContext _db;
        private readonly IOrganizationScopeProjection _organizationProjection;

        public GetLoginQueryHandler(
            IJwtTokenService jwtTokenService,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IUnitOfWork unitOfWork,
            IOptions<LoginSessionOptions> sessionOptions,
            ApplicationDbContext db,
            IOrganizationScopeProjection organizationProjection)
        {
            _jwtTokenService = jwtTokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _sessionOptions = sessionOptions.Value;
            _db = db;
            _organizationProjection = organizationProjection;
        }

        public async Task<Result<Response.Authenticated>> Handle(Query.Login request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return await InvalidCredentialsAsync(null, "UnknownAccount", cancellationToken);
            if (!user.IsActive)
                return await InvalidCredentialsAsync(user, "InactiveAccount", cancellationToken);

            if (await _userManager.IsLockedOutAsync(user))
            {
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
                return await InvalidCredentialsAsync(user, "AccountLocked", cancellationToken);
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                await RecordFailedAttemptAsync(user, cancellationToken);
                return await InvalidCredentialsAsync(user, "InvalidCredentials", cancellationToken);
            }

            if (!user.EmailConfirmed)
                return await InvalidCredentialsAsync(user, "EmailUnconfirmed", cancellationToken);

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                var code = request.TwoFactorCode?.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (string.IsNullOrWhiteSpace(code))
                {
                    _unitOfWork.SecurityAuditRepository.Record(
                        SecurityAuditEventTypes.Login,
                        SecurityAuditOutcomes.Failed,
                        user.Id,
                        "TwoFactorRequired", tenantId: user.TenantId);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    return Result.Failure<Response.Authenticated>(new Error(
                        "Identity.Authentication.TwoFactorRequired",
                        "Enter the authenticator code to complete sign-in."));
                }

                var isValidCode = !string.IsNullOrWhiteSpace(code)
                    && await _userManager.VerifyTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        code);

                if (!isValidCode)
                {
                    await RecordFailedAttemptAsync(user, cancellationToken);
                    return await InvalidCredentialsAsync(user, "InvalidTwoFactorCode", cancellationToken);
                }
            }

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

            await _userManager.ResetAccessFailedCountAsync(user);

            var sessionId = Guid.NewGuid();
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var now = DateTimeOffset.UtcNow;
            var refreshTokenExpiry = now.AddDays(30);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
            {
                // Serialize only logins of the same account; avoid range-lock upgrade deadlocks.
                if (_db.Database.IsSqlServer())
                {
                    var resource = $"Identity.Login:{user.Id}";
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        DECLARE @result int;
                        EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
                        IF @result < 0 THROW 51000, 'Could not acquire the account login lock.', 1;", transactionToken);
                }
                var activeSessions = await _unitOfWork.LoginSessionRepository
                    .FindAll(item => item.UserId == user.Id && !item.IsRevoked && item.ExpiresAtUtc > now)
                    .AsTracking()
                    .OrderBy(item => item.LastActiveAtUtc).ThenBy(item => item.Id)
                    .ToListAsync(transactionToken);
                var removed = activeSessions.Take(Math.Max(0, activeSessions.Count - _sessionOptions.MaxConcurrentSessions + 1)).ToList();
                foreach (var previous in removed)
                {
                    previous.Revoke(now);
                    _unitOfWork.LoginSessionRepository.Update(previous);
                }
                if (removed.Count > 0)
                    _unitOfWork.SecurityAuditRepository.Record(SecurityAuditEventTypes.SessionsRevoked,
                        SecurityAuditOutcomes.Succeeded, user.Id, $"SessionLimit:{removed.Count}", tenantId: user.TenantId);
                _unitOfWork.LoginSessionRepository.Add(new LoginSession(
                    sessionId,
                    user.Id,
                    HashToken(refreshToken),
                    request.UserAgent,
                    request.IpAddress,
                    now,
                    refreshTokenExpiry));

                var previousIpAddress = string.IsNullOrWhiteSpace(request.IpAddress)
                    ? null
                    : await _unitOfWork.SecurityAuditRepository.GetLatestSuccessfulLoginIpAsync(
                        user.Id,
                        transactionToken);
                _unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.Login,
                    SecurityAuditOutcomes.Succeeded,
                    user.Id,
                    sessionId: sessionId,
                    tenantId: user.TenantId);
                if (previousIpAddress is not null
                    && !string.Equals(previousIpAddress, request.IpAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _unitOfWork.SecurityAuditRepository.Record(
                        SecurityAuditEventTypes.NewLoginIp,
                        SecurityAuditOutcomes.Succeeded,
                        user.Id,
                        "LoginFromNewIp",
                        sessionId,
                        user.TenantId);
                }
                await _unitOfWork.SaveChangeAsync(transactionToken);
            }, cancellationToken);

            return Result.Success(await CreateAuthenticatedResponseAsync(user, sessionId, refreshToken, refreshTokenExpiry));
        }

        internal async Task<Response.Authenticated> CreateAuthenticatedResponseAsync(
            AppUser user,
            Guid sessionId,
            string refreshToken,
            DateTimeOffset refreshTokenExpiry)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Sid, sessionId.ToString()),
                new(IdentityClaimTypes.TenantId, user.TenantId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(await IdentityClaimsFactory.GetCustomClaimsAsync(
                user,
                roles,
                _userManager,
                _roleManager));

            return new Response.Authenticated
            {
                UserId = user.Id,
                SessionId = sessionId,
                AccessToken = _jwtTokenService.GenerateAccessToken(claims),
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiry.UtcDateTime
            };
        }

        internal static string HashToken(string token) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        private async Task RecordFailedAttemptAsync(AppUser user, CancellationToken cancellationToken)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
                await RefreshTokenRevoker.RevokeAllAsync(_unitOfWork, user.Id, cancellationToken);
        }

        private async Task<Result<Response.Authenticated>> InvalidCredentialsAsync(
            AppUser? user,
            string reason,
            CancellationToken cancellationToken)
        {
            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.Login,
                SecurityAuditOutcomes.Failed,
                user?.Id,
                reason,
                tenantId: user?.TenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result.Failure<Response.Authenticated>(
                new Error("Identity.Authentication.InvalidCredentials", "Invalid email or password."));
        }

    }
}
