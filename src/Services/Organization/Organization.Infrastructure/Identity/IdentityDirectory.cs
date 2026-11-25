using System.Net;
using System.Net.Http.Json;
using Organization.Application.Abstractions;

namespace Organization.Infrastructure.Identity
{
    public sealed class IdentityDirectory : IIdentityDirectory
    {
        private readonly HttpClient _client;

        public IdentityDirectory(HttpClient client)
        {
            _client = client;
        }

        public async Task<bool> UserExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
        {
            using var response = await _client.GetAsync($"internal/v1/users/{userId}/tenants/{tenantId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
    }
}
