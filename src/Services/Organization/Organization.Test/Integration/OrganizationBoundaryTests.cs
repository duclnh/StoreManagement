using BuildingBlocks.Contracts.Shared;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Organization.Application.Abstractions;
using Organization.Application.DependencyInjection.Extensions;
using Organization.Domain.Abstractions;
using Organization.Domain.Entities.Branches;
using Organization.Domain.Entities.Members;
using Organization.Domain.Entities.Tenants;
using Organization.Domain.Entities.Warehouses;
using Organization.Domain.Repositories;
using Organization.Persistence;
using Organization.Persistence.Repositories;
using Xunit;
using BranchCommand = Organization.Contract.Services.V1.Branches.Command;
using TenantCommand = Organization.Contract.Services.V1.Tenants.Command;
using TenantQuery = Organization.Contract.Services.V1.Tenants.Query;
using OrganizationEntity = global::Organization.Domain.Entities.Organizations.Organization;
using MemberCommand = Organization.Contract.Services.V1.Members.Command;
using WarehouseCommand = Organization.Contract.Services.V1.Warehouses.Command;

namespace Organization.Test.Integration
{
    public sealed class OrganizationBoundaryTests
    {
        [Fact]
        public async Task TenantAdministratorCannotReadOrUpdateAnotherTenant()
        {
            await using var fixture = await Fixture.CreateAsync();
            fixture.Context.IsSystemAdministrator = false;
            fixture.Context.IsTenantAdministrator = true;
            fixture.Context.TenantId = fixture.TenantA.Id;
            var repository = fixture.Services.GetRequiredService<IRepository<Branch>>();
            Assert.Null(await repository.GetAsync(fixture.BranchB.Id, default));
            var result = await fixture.Sender.Send(new BranchCommand.SetBranchStatus(fixture.BranchB.Id, false));
            Assert.True(result.IsFailure);
            Assert.True(await fixture.Db.Branches.Where(x => x.Id == fixture.BranchB.Id).Select(x => x.Status).SingleAsync());
        }

        [Fact]
        public async Task MissingTenantFailsClosed()
        {
            await using var fixture = await Fixture.CreateAsync();
            fixture.Context.IsSystemAdministrator = false;
            fixture.Context.TenantId = null;
            var result = await fixture.Sender.Send(new TenantQuery.ListTenants());
            Assert.True(result.IsFailure);
            Assert.Empty(await fixture.Services.GetRequiredService<IRepository<Branch>>().ListAsync(null, null, 1, 20, default));
        }

        [Fact]
        public async Task BranchManagerCannotAccessSiblingBranchOrChangeOrganizationMembership()
        {
            await using var fixture = await Fixture.CreateAsync();
            var sibling = new Branch { TenantId = fixture.TenantA.Id, OrganizationId = fixture.OrganizationA.Id, Code = "SIBLING", Name = "Sibling" };
            fixture.Db.Branches.Add(sibling);
            fixture.Db.Members.Add(new OrganizationMember
            {
                TenantId = fixture.TenantA.Id,
                OrganizationId = fixture.OrganizationA.Id,
                BranchId = fixture.BranchA.Id,
                UserId = fixture.Context.UserId!.Value
            });
            await fixture.Db.SaveChangesAsync();
            fixture.Context.IsSystemAdministrator = false;
            fixture.Context.IsBranchManager = true;
            fixture.Context.TenantId = fixture.TenantA.Id;

            var allowed = await fixture.Sender.Send(new BranchCommand.SetBranchStatus(fixture.BranchA.Id, false));
            var denied = await fixture.Sender.Send(new BranchCommand.SetBranchStatus(sibling.Id, false));
            var membership = await fixture.Sender.Send(new MemberCommand.AddOrganizationMember(fixture.OrganizationA.Id, new(Guid.NewGuid())));
            Assert.True(allowed.IsSuccess);
            Assert.True(denied.IsFailure);
            Assert.True(membership.IsFailure);
        }

        [Fact]
        public async Task RemovedMembershipImmediatelyRemovesAccess()
        {
            await using var fixture = await Fixture.CreateAsync();
            var member = new OrganizationMember { TenantId = fixture.TenantA.Id, OrganizationId = fixture.OrganizationA.Id, BranchId = fixture.BranchA.Id, UserId = fixture.Context.UserId!.Value };
            fixture.Db.Members.Add(member);
            await fixture.Db.SaveChangesAsync();
            fixture.Context.IsSystemAdministrator = false;
            fixture.Context.IsBranchManager = true;
            fixture.Context.TenantId = fixture.TenantA.Id;
            var repository = fixture.Services.GetRequiredService<IRepository<Branch>>();
            Assert.NotNull(await repository.GetAsync(fixture.BranchA.Id, default));
            fixture.Db.Members.Remove(member);
            await fixture.Db.SaveChangesAsync();
            Assert.Null(await repository.GetAsync(fixture.BranchA.Id, default));
        }

        [Fact]
        public async Task CompositeForeignKeyRejectsCrossTenantWarehouse()
        {
            await using var fixture = await Fixture.CreateAsync();
            fixture.Db.Warehouses.Add(new Warehouse { TenantId = fixture.TenantA.Id, BranchId = fixture.BranchB.Id, Code = "CROSS", Name = "Cross" });
            await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
        }

        [Fact]
        public async Task DuplicateCodeIsScopedToParentAndDatabaseEnforcesIt()
        {
            await using var fixture = await Fixture.CreateAsync();
            fixture.Db.Warehouses.AddRange(
                new Warehouse { TenantId = fixture.TenantA.Id, BranchId = fixture.BranchA.Id, Code = "SAME", Name = "First" },
                new Warehouse { TenantId = fixture.TenantB.Id, BranchId = fixture.BranchB.Id, Code = "SAME", Name = "Second" });
            await fixture.Db.SaveChangesAsync();
            fixture.Db.Warehouses.Add(new Warehouse { TenantId = fixture.TenantA.Id, BranchId = fixture.BranchA.Id, Code = "SAME", Name = "Duplicate" });
            await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
        }

        [Fact]
        public async Task MarkerCommandCommitsAndSwitchingDefaultKeepsOneDefault()
        {
            await using var fixture = await Fixture.CreateAsync();
            var first = await fixture.Sender.Send(new WarehouseCommand.CreateWarehouse(fixture.BranchA.Id, new(" FIRST ", "First", "MAIN", null, true)));
            var second = await fixture.Sender.Send(new WarehouseCommand.CreateWarehouse(fixture.BranchA.Id, new("SECOND", "Second", "RETAIL", null, true)));
            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            fixture.Db.ChangeTracker.Clear();
            Assert.Equal(2, await fixture.Db.Warehouses.CountAsync());
            Assert.Equal(1, await fixture.Db.Warehouses.CountAsync(x => x.IsDefault));
            Assert.Equal("FIRST", (await fixture.Db.Warehouses.FindAsync(first.Value!.Id))!.Code);
        }

        [Fact]
        public async Task InvalidConfigurationDoesNotWriteAndSearchIsPaged()
        {
            await using var fixture = await Fixture.CreateAsync();
            var before = await fixture.Db.Tenants.CountAsync();
            var invalid = await fixture.Sender.Send(new TenantCommand.CreateTenant(new("INVALID", "Invalid", null, null, "[]")));
            Assert.IsAssignableFrom<IValidationResult>(invalid);
            Assert.Equal(before, await fixture.Db.Tenants.CountAsync());
            var result = await fixture.Sender.Send(new TenantQuery.ListTenants(Search: "Tenant A", PageSize: 1));
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!.Items);
            Assert.Equal(fixture.TenantA.Id, result.Value.Items[0].Id);
        }

        [Fact]
        public async Task FailedCommandRollsBackTrackedMutations()
        {
            await using var fixture = await Fixture.CreateAsync();
            var other = new Branch { TenantId = fixture.TenantA.Id, OrganizationId = fixture.OrganizationA.Id, Code = "OTHER", Name = "Other" };
            fixture.Db.Branches.Add(other);
            await fixture.Db.SaveChangesAsync();
            var result = await fixture.Sender.Send(new BranchCommand.UpdateBranch(fixture.BranchA.Id,
                new("OTHER", "Should not persist", null, null, null, null, false)));
            Assert.True(result.IsFailure);
            fixture.Db.ChangeTracker.Clear();
            Assert.Equal("Branch A", (await fixture.Db.Branches.FindAsync(fixture.BranchA.Id))!.Name);
        }

        [Fact]
        public async Task MembershipCannotReferenceUserOutsideTenant()
        {
            await using var fixture = await Fixture.CreateAsync();
            fixture.Directory.Exists = false;
            var result = await fixture.Sender.Send(new MemberCommand.AssignBranchMember(fixture.BranchA.Id, new(Guid.NewGuid())));
            Assert.True(result.IsFailure);
            Assert.Empty(await fixture.Db.Members.ToListAsync());
        }

        private sealed class Fixture : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;
            public ServiceProvider Services { get; }
            public TestContext Context { get; } = new();
            public TestDirectory Directory { get; } = new();
            public ApplicationDbContext Db => Services.GetRequiredService<ApplicationDbContext>();
            public ISender Sender => Services.GetRequiredService<ISender>();
            public Tenant TenantA { get; } = new() { Code = "A", Name = "Tenant A" };
            public Tenant TenantB { get; } = new() { Code = "B", Name = "Tenant B" };
            public OrganizationEntity OrganizationA { get; } = new() { Code = "A", Name = "Organization A" };
            public Branch BranchA { get; } = new() { Code = "A", Name = "Branch A" };
            public Branch BranchB { get; } = new() { Code = "B", Name = "Branch B" };

            private Fixture(SqliteConnection connection)
            {
                _connection = connection;
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ITenantContext>(Context);
                services.AddSingleton<IOrganizationContext>(Context);
                services.AddSingleton<IIdentityDirectory>(Directory);
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                services.AddScoped<IMemberRepository, MemberRepository>();
                services.AddScoped<ICommandTransaction, CommandTransaction>();
                services.AddScoped<IUnitOfWork, EFUnitOfWork>();
                services.AddOrganizationApplication();
                Services = services.BuildServiceProvider();
            }

            public static async Task<Fixture> CreateAsync()
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();
                var fixture = new Fixture(connection);
                await fixture.Db.Database.EnsureCreatedAsync();
                fixture.TenantA.TenantId = fixture.TenantA.Id;
                fixture.TenantB.TenantId = fixture.TenantB.Id;
                fixture.OrganizationA.TenantId = fixture.TenantA.Id;
                var organizationB = new OrganizationEntity { TenantId = fixture.TenantB.Id, Code = "B", Name = "Organization B" };
                fixture.BranchA.TenantId = fixture.TenantA.Id;
                fixture.BranchA.OrganizationId = fixture.OrganizationA.Id;
                fixture.BranchB.TenantId = fixture.TenantB.Id;
                fixture.BranchB.OrganizationId = organizationB.Id;
                fixture.Db.AddRange(fixture.TenantA, fixture.TenantB, fixture.OrganizationA, organizationB, fixture.BranchA, fixture.BranchB);
                await fixture.Db.SaveChangesAsync();
                return fixture;
            }

            public async ValueTask DisposeAsync()
            {
                await Services.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private sealed class TestContext : ITenantContext, IOrganizationContext
        {
            public Guid? TenantId { get; set; }
            public Guid? UserId { get; set; } = Guid.NewGuid();
            public bool IsSystemAdministrator { get; set; } = true;
            public bool IsTenantAdministrator { get; set; }
            public bool IsBranchManager { get; set; }
            public Guid? OrganizationId { get; set; }
            public Guid? BranchId { get; set; }
            public bool HasPermission(string permission) => false;
        }

        private sealed class TestDirectory : IIdentityDirectory
        {
            public bool Exists { get; set; } = true;
            public Task<bool> UserExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(Exists);
        }
    }
}
