using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable(TableNames.Tenants);
            builder.HasKey(tenant => tenant.Id);
            builder.Property(tenant => tenant.Code).HasMaxLength(32).IsRequired();
            builder.Property(tenant => tenant.Name).HasMaxLength(150).IsRequired();
            builder.Property(tenant => tenant.IsActive).IsRequired();
            builder.Property(tenant => tenant.CreatedAtUtc).IsRequired();
            builder.Property(tenant => tenant.StartsAtUtc).IsRequired();
            builder.Property(tenant => tenant.ContactName).HasMaxLength(150);
            builder.Property(tenant => tenant.ContactEmail).HasMaxLength(254);
            builder.Property(tenant => tenant.ContactPhone).HasMaxLength(32);
            builder.Property(tenant => tenant.ConfigurationJson).HasColumnType("nvarchar(max)").IsRequired();
            builder.HasIndex(tenant => tenant.Code).IsUnique();
            builder.HasIndex(tenant => tenant.Name);
        }
    }
}
