using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entity = global::Organization.Domain.Entities.Tenants.Tenant;


namespace Organization.Persistence.Configurations
{
    public sealed class TenantConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable("Tenants", table => table.HasCheckConstraint("CK_Tenant_Self", "[TenantId] = [Id]"));
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.Code }).IsUnique();

            builder.Property(x => x.ContactEmail).HasMaxLength(254);
            builder.Property(x => x.ContactPhone).HasMaxLength(32);
            builder.Property(x => x.ConfigurationJson).HasMaxLength(65536).IsRequired();

        }
    }
}
