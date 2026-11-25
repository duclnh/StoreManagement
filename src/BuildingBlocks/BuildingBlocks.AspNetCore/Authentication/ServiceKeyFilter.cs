using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.AspNetCore.Authentication
{
    public sealed class ServiceKeyFilter : IEndpointFilter
    {
        private readonly IConfiguration _configuration;

        public ServiceKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var expected = _configuration["ServiceAuthentication:Key"];
            var actual = context.HttpContext.Request.Headers["X-Service-Key"].ToString();
            if (string.IsNullOrEmpty(expected) || expected.Length < 32 || actual.Length > 4096
                || !CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(actual))))
            {
                return Results.Unauthorized();
            }

            return await next(context);
        }
    }
}
