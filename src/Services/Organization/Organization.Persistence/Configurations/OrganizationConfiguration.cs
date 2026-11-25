using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entity = global::Organization.Domain.Entities.Organizations.Organization;
using Parent = global::Organization.Domain.Entities.Tenants.Tenant;

namespace Organization.Persistence.Configurations
{
    public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable("Organizations");
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            builder.HasOne<Parent>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.TaxCode).HasMaxLength(500);
            builder.Property(x => x.BusinessType).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Industry).HasMaxLength(500);
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.Property(x => x.Phone).HasMaxLength(32);
            builder.Property(x => x.Email).HasMaxLength(254);
            builder.Property(x => x.LogoUrl).HasMaxLength(2048);
            builder.Property(x => x.Currency).HasMaxLength(500).IsRequired();
            builder.Property(x => x.TimeZone).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Language).HasMaxLength(500).IsRequired();

        }
    }
}
