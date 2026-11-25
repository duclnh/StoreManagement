using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entity = global::Organization.Domain.Entities.Warehouses.Warehouse;
using Parent = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Persistence.Configurations
{
    public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable("Warehouses");
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.TenantId, x.BranchId, x.Code }).IsUnique();
            builder.HasOne<Parent>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.WarehouseType).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.HasIndex(x => new { x.TenantId, x.BranchId }).IsUnique().HasFilter("[IsDefault] = 1");
        }
    }
}
