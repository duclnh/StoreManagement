using Microsoft.EntityFrameworkCore;
using Organization.Domain.Abstractions;
using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Entities.Members;
using TenantEntity = global::Organization.Domain.Entities.Tenants.Tenant;
using OrganizationEntity = global::Organization.Domain.Entities.Organizations.Organization;
using BranchEntity = global::Organization.Domain.Entities.Branches.Branch;
using WarehouseEntity = global::Organization.Domain.Entities.Warehouses.Warehouse;
using PointOfSaleEntity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Persistence
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
        public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
        public DbSet<BranchEntity> Branches => Set<BranchEntity>();
        public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();
        public DbSet<PointOfSaleEntity> PointOfSales => Set<PointOfSaleEntity>();
        public DbSet<OrganizationMember> Members => Set<OrganizationMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("organization");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public async Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<ScopedEntity>().Where(x => x.State == EntityState.Modified))
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return await SaveChangesAsync(cancellationToken);
        }
    }
}
