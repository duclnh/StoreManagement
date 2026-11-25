using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Organization.Persistence;
using Xunit;

namespace Organization.Test.Integration
{
    public sealed class OrganizationApiTests
    {
        [Fact]
        public async Task HierarchyAndMembershipRoutesPersistAndReturnScopedDtos()
        {
            await using var factory = new ApiFactory();
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrator", Guid.NewGuid()));
            var tenantResponse = await client.PostAsJsonAsync("/api/v1/tenants", new { code = "T", name = "Tenant", configurationJson = "{}" });
            var tenant = await tenantResponse.Content.ReadFromJsonAsync<Contract.Services.V1.Tenants.Response.Detail>();
            var organizationResponse = await client.PostAsJsonAsync($"/api/v1/organizations?tenantId={tenant!.Id}", new
            {
                code = "ORG", name = "Company", businessType = "ELECTRONICS", currency = "VND", timeZone = "Asia/Ho_Chi_Minh", language = "vi"
            });
            Assert.Equal(HttpStatusCode.Created, organizationResponse.StatusCode);
            var organization = await organizationResponse.Content.ReadFromJsonAsync<Contract.Services.V1.Organizations.Response.Detail>();
            var branchResponse = await client.PostAsJsonAsync($"/api/v1/organizations/{organization!.Id}/branches", new
            {
                code = "BR", name = "Branch", isDefault = true
            });
            Assert.Equal(HttpStatusCode.Created, branchResponse.StatusCode);
            var branch = await branchResponse.Content.ReadFromJsonAsync<Contract.Services.V1.Branches.Response.Detail>();
            var warehouseResponse = await client.PostAsJsonAsync($"/api/v1/branches/{branch!.Id}/warehouses", new
            {
                code = "WH", name = "Warehouse", warehouseType = "MAIN", isDefault = true
            });
            Assert.Equal(HttpStatusCode.Created, warehouseResponse.StatusCode);
            var warehouse = await warehouseResponse.Content.ReadFromJsonAsync<Contract.Services.V1.Warehouses.Response.Detail>();
            Assert.Equal(tenant.Id, warehouse!.TenantId);
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/warehouses/{warehouse.Id}", new
            {
                code = "WH", name = "Warehouse renamed", warehouseType = "RETURN", isDefault = true
            })).StatusCode);
            var posResponse = await client.PostAsJsonAsync($"/api/v1/branches/{branch.Id}/pos", new { code = "POS", name = "Counter" });
            Assert.Equal(HttpStatusCode.Created, posResponse.StatusCode);
            var pos = await posResponse.Content.ReadFromJsonAsync<Contract.Services.V1.PointOfSales.Response.Detail>();
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/pos/{pos!.Id}", new { code = "POS", name = "Counter updated" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/branches/{branch.Id}/warehouses")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/branches/{branch.Id}/pos")).StatusCode);
            var userId = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/organizations/{organization.Id}/members", new { userId })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/branches/{branch.Id}/members", new { userId })).StatusCode);
            var members = await client.GetFromJsonAsync<Contract.Shared.Page<Contract.Services.V1.Members.Response.Detail>>($"/api/v1/organizations/{organization.Id}/members");
            Assert.Equal(2, members!.TotalCount);
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/organizations/{organization.Id}/members/{userId}")).StatusCode);
            var branchMembers = await client.GetFromJsonAsync<Contract.Shared.Page<Contract.Services.V1.Members.Response.Detail>>($"/api/v1/branches/{branch.Id}/members");
            Assert.Empty(branchMembers!.Items);
        }

        [Fact]
        public async Task PublicRoutesRequireIdentityAuthentication()
        {
            await using var factory = new ApiFactory();
            var client = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/tenants")).StatusCode);
        }

        [Fact]
        public async Task AdminCanCreateSearchReadUpdateAndDeactivateTenant()
        {
            await using var factory = new ApiFactory();
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrator", Guid.NewGuid()));
            var created = await client.PostAsJsonAsync("/api/v1/tenants", new
            {
                code = "API",
                name = "API Tenant",
                contactEmail = "owner@example.com",
                configurationJson = "{\"tax\":true}"
            });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var tenant = await created.Content.ReadFromJsonAsync<Contract.Services.V1.Tenants.Response.Detail>();
            Assert.NotNull(tenant);
            Assert.Equal(tenant.Id, tenant.TenantId);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants/{tenant.Id}")).StatusCode);
            var search = await client.GetFromJsonAsync<Contract.Shared.Page<Contract.Services.V1.Tenants.Response.Detail>>("/api/v1/tenants?search=API&pageSize=1");
            Assert.Single(search!.Items);
            var updated = await client.PutAsJsonAsync($"/api/v1/tenants/{tenant.Id}", new
            {
                code = "API",
                name = "Renamed",
                configurationJson = "{}"
            });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var status = await client.PatchAsJsonAsync($"/api/v1/tenants/{tenant.Id}/status", new { status = false });
            Assert.Equal(HttpStatusCode.OK, status.StatusCode);
            Assert.False((await status.Content.ReadFromJsonAsync<Contract.Services.V1.Tenants.Response.Detail>())!.Status);
        }

        [Fact]
        public async Task InvalidTenantReturnsValidationProblemWithoutWriting()
        {
            await using var factory = new ApiFactory();
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrator", Guid.NewGuid()));
            var response = await client.PostAsJsonAsync("/api/v1/tenants", new { code = "", name = "", configurationJson = "[]" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("errors", await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Tenants.ToListAsync());
        }

        [Fact]
        public async Task RevokedIdentitySessionAndWrongIssuerAreRejected()
        {
            await using var factory = new ApiFactory();
            factory.IdentityAcceptsSession = false;
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrator", Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/tenants")).StatusCode);
            factory.IdentityAcceptsSession = true;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrator", Guid.NewGuid(), issuer: "untrusted"));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/tenants")).StatusCode);
        }

        [Fact]
        public async Task InternalDirectoryRequiresServiceCredential()
        {
            await using var factory = new ApiFactory();
            var client = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/internal/v1/tenants")).StatusCode);
            client.DefaultRequestHeaders.Add("X-Service-Key", ApiFactory.Secret);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/internal/v1/tenants")).StatusCode);
        }

        private static string Token(string role, Guid tenantId, string issuer = "identity-tests")
        {
            var token = new JwtSecurityToken(issuer, "store-tests", new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Sid, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("tenant_id", tenantId.ToString())
            }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.Secret)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            public const string Secret = "organization-tests-signing-key-with-at-least-32-characters";
            private readonly SqliteConnection _connection = new("Data Source=:memory:");
            public bool IdentityAcceptsSession { get; set; } = true;

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                _connection.Open();
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                foreach (var setting in new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Organization"] = "Server=(localdb)\\MSSQLLocalDB;Database=Unused;Trusted_Connection=True;",
                    ["Services:Identity:BaseUrl"] = "https://identity.test/",
                    ["ServiceAuthentication:Key"] = Secret,
                    ["JwtOptions:SecretKey"] = Secret,
                    ["JwtOptions:Issuer"] = "identity-tests",
                    ["JwtOptions:Audience"] = "store-tests"
                })
                {
                    builder.UseSetting(setting.Key, setting.Value);
                }
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<ApplicationDbContext>();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                    services.AddSingleton<IHttpClientFactory>(new TestClientFactory(() => IdentityAcceptsSession));
                    using var provider = services.BuildServiceProvider();
                    using var scope = provider.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
                });
            }

            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private sealed class TestClientFactory : IHttpClientFactory
        {
            private readonly Func<bool> _accepts;

            public TestClientFactory(Func<bool> accepts)
            {
                _accepts = accepts;
            }

            public HttpClient CreateClient(string name) => new(new TestIdentityHandler(_accepts)) { BaseAddress = new Uri("https://identity.test/") };
        }

        private sealed class TestIdentityHandler : HttpMessageHandler
        {
            private readonly Func<bool> _accepts;

            public TestIdentityHandler(Func<bool> accepts)
            {
                _accepts = accepts;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(_accepts() ? HttpStatusCode.OK : HttpStatusCode.Unauthorized));
        }
    }
}
