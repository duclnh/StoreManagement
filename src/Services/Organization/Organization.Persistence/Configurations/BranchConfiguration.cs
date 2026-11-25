using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entity = global::Organization.Domain.Entities.Branches.Branch;
using Parent = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Persistence.Configurations
{
    public sealed class BranchConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable("Branches");
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.TenantId, x.OrganizationId, x.Id });
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.TenantId, x.OrganizationId, x.Code }).IsUnique();
            builder.HasOne<Parent>().WithMany().HasForeignKey(x => new { x.TenantId, x.OrganizationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.Property(x => x.Phone).HasMaxLength(32);
            builder.Property(x => x.Email).HasMaxLength(254);
            builder.HasIndex(x => new { x.TenantId, x.OrganizationId }).IsUnique().HasFilter("[IsDefault] = 1");
        }
    }
}
