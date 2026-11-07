using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable(TableNames.Organizations);
            builder.HasKey(organization => organization.Id);
            builder.Property(organization => organization.Name).HasMaxLength(150).IsRequired();
            builder.Property(organization => organization.IsActive).IsRequired();
            builder.HasIndex(organization => new { organization.TenantId, organization.Name }).IsUnique();
            builder.HasOne(organization => organization.Tenant)
                .WithMany(tenant => tenant.Organizations)
                .HasForeignKey(organization => organization.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
