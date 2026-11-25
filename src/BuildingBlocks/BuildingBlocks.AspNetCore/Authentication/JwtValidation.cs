using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.AspNetCore.Authentication
{
    public static class JwtValidation
    {
        public static TokenValidationParameters Create(string issuer, string audience, string secret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
            ArgumentException.ThrowIfNullOrWhiteSpace(audience);
            ArgumentException.ThrowIfNullOrWhiteSpace(secret);
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
