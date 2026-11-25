using System.Net;
using System.Net.Http.Json;
using Identity.Domain.Abstractions;
using Snapshot = global::Organization.Contract.Services.V1.Directory.Response.Snapshot;

namespace Identity.Infrastructure.Organization
{
    public sealed class OrganizationScopeDirectory : IOrganizationScopeDirectory
    {
        private readonly HttpClient _client;

        public OrganizationScopeDirectory(HttpClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<TenantScopeSnapshot>> GetTenantsAsync(CancellationToken cancellationToken)
        {
            var tenants = await _client.GetFromJsonAsync<List<global::Organization.Contract.Services.V1.Directory.Response.TenantScope>>(
                "internal/v1/tenants", cancellationToken) ?? throw new HttpRequestException("Organization returned no tenant directory.");
            return tenants.Select(x => new TenantScopeSnapshot(x.Id, x.Code, x.Name, x.Status, x.CreatedAt)).ToArray();
        }

        public async Task<OrganizationScopeSnapshotSet?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            using var response = await _client.GetAsync($"internal/v1/tenants/{tenantId}/snapshot", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var snapshot = await response.Content.ReadFromJsonAsync<Snapshot>(cancellationToken: cancellationToken)
                ?? throw new HttpRequestException("Organization returned an empty directory response.");
            if (snapshot.Tenant.Id != tenantId || snapshot.Organizations.Any(x => x.TenantId != tenantId)
                || snapshot.Branches.Any(x => !snapshot.Organizations.Any(organization => organization.Id == x.OrganizationId)))
            {
                throw new HttpRequestException("Organization returned an invalid scope boundary.");
            }

            return new OrganizationScopeSnapshotSet(
                new TenantScopeSnapshot(snapshot.Tenant.Id, snapshot.Tenant.Code, snapshot.Tenant.Name, snapshot.Tenant.Status, snapshot.Tenant.CreatedAt),
                snapshot.Organizations.Select(x => new OrganizationScopeSnapshot(x.Id, x.TenantId, x.Name, x.Status)).ToArray(),
                snapshot.Branches.Select(x => new BranchScopeSnapshot(x.Id, x.OrganizationId, x.Name, x.Status)).ToArray());
        }
    }
}
