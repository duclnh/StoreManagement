using Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Identity.Test.Integration;

public sealed class IdentityModelTests
{
    [Fact]
    public void Model_HasNoPendingMigrationChanges()
    {
        using var db = CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void LatestMigration_ContainsSessionAndAuditScopeColumns()
    {
        using var db = CreateContext();
        var migrator = db.GetService<IMigrator>();
        var script = migrator.GenerateScript("20261107150912_AddTenantScopeFoundation", "20261122000100_AddAuditScope");

        Assert.Contains("ProtectedRefreshToken", script);
        Assert.Contains("ScopeId", script);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=IdentityTest;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        return new ApplicationDbContext(options);
    }
}
