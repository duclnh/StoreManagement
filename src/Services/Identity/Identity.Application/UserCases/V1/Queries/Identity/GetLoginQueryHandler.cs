using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class GetLoginQueryHandler : IQueryHandler<Query.Login, Response.Authenticated>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetLoginQueryHandler(
            IJwtTokenService jwtTokenService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _jwtTokenService = jwtTokenService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Response.Authenticated>> Handle(Query.Login request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive)
                return InvalidCredentials();

            if (await _userManager.IsLockedOutAsync(user) || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                if (!await _userManager.IsLockedOutAsync(user))
                    await _userManager.AccessFailedAsync(user);

                return InvalidCredentials();
            }

            if (!user.EmailConfirmed)
                return InvalidCredentials();

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                var code = request.TwoFactorCode?.Replace(" ", string.Empty).Replace("-", string.Empty);
                var isValidCode = !string.IsNullOrWhiteSpace(code)
                    && await _userManager.VerifyTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        code);

                if (!isValidCode)
                {
                    await _userManager.AccessFailedAsync(user);
                    return InvalidCredentials();
                }
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var sessionId = Guid.NewGuid();
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var now = DateTimeOffset.UtcNow;
            var refreshTokenExpiry = now.AddDays(30);
            _unitOfWork.LoginSessionRepository.Add(new LoginSession(
                sessionId,
                user.Id,
                HashToken(refreshToken),
                request.UserAgent,
                request.IpAddress,
                now,
                refreshTokenExpiry));
            await _unitOfWork.SaveChangeAsync(cancellationToken);

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
                new(ClaimTypes.Sid, sessionId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

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

        private static Result<Response.Authenticated> InvalidCredentials() =>
            Result.Failure<Response.Authenticated>(new Error("Identity.Authentication.InvalidCredentials", "Invalid email or password."));

    }
}


