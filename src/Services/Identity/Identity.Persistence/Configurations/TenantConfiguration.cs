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
            builder.Property(tenant => tenant.Name).HasMaxLength(150).IsRequired();
            builder.Property(tenant => tenant.IsActive).IsRequired();
            builder.Property(tenant => tenant.CreatedAtUtc).IsRequired();
            builder.HasIndex(tenant => tenant.Name).IsUnique();
        }
    }
}
