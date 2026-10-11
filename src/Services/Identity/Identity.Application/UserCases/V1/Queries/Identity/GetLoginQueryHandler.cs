using Identity.Application.Abstractions;
using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class GetLoginQueryHandler : IQueryHandler<Query.Login, Response.Authenticated>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICacheService _cacheService;

        public GetLoginQueryHandler(IJwtTokenService jwtTokenService, ICacheService cacheService)
        {
            _jwtTokenService = jwtTokenService;
            _cacheService = cacheService;
        }

        public async Task<Result<Response.Authenticated>> Handle(Query.Login request, CancellationToken cancellationToken)
        {
            //check user


            //generate JwtToken

            var claims = new List<Claim> { 
                new Claim(ClaimTypes.Email, request.Email),
                new Claim(ClaimTypes.Role, "Senior .NET leader")
            };
                
            var accessToken = _jwtTokenService.GenerateAccessToken(claims);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var response = new Response.Authenticated()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.Now.AddMinutes(5)
            };

            await _cacheService.SetAsync("lengochuynduc8@gmail.com", response);


            return Result.Success(response);
        }
    }
}


