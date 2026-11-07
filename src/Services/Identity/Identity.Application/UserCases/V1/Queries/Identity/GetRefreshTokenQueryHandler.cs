using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class GetRefreshTokenQueryHandler : IQueryHandler<Query.RefreshToken, Response.Authenticated>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetRefreshTokenQueryHandler(
            IJwtTokenService jwtTokenService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _jwtTokenService = jwtTokenService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Response.Authenticated>> Handle(Query.RefreshToken request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return InvalidRefreshToken();

            var session = await _unitOfWork.LoginSessionRepository.FindByIdAsync(request.SessionId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            if (session is null
                || session.UserId != user.Id
                || session.IsRevoked
                || session.ExpiresAtUtc <= now)
                return InvalidRefreshToken();

            var suppliedHash = Encoding.ASCII.GetBytes(GetLoginQueryHandler.HashToken(request.Token));
            var storedHash = Encoding.ASCII.GetBytes(session.RefreshTokenHash);
            if (storedHash.Length != suppliedHash.Length || !CryptographicOperations.FixedTimeEquals(storedHash, suppliedHash))
                return InvalidRefreshToken();

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = now.AddDays(30);
            session.Rotate(GetLoginQueryHandler.HashToken(refreshToken), refreshTokenExpiry, now);
            _unitOfWork.LoginSessionRepository.Update(session);
            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return InvalidRefreshToken();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Sid, request.SessionId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

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


