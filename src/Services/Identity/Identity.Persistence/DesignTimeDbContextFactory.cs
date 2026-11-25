using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Persistence
{
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnectString")
                    ?? "Server=(localdb)\\MSSQLLocalDB;Database=StoreManagement.Identity;Trusted_Connection=True;")
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
