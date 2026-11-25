using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Organization.Persistence
{
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Organization")
                    ?? "Server=(localdb)\\MSSQLLocalDB;Database=StoreManagement.Organization;Trusted_Connection=True;",
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "organization"))
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
