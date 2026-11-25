using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;
using Parent = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Persistence.Configurations
{
    public sealed class PointOfSaleConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable("PointOfSales");
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.TenantId, x.BranchId, x.Code }).IsUnique();
            builder.HasOne<Parent>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.Description).HasMaxLength(2000);

        }
    }
}
