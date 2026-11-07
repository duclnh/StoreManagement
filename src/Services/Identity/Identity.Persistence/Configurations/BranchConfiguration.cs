using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable(TableNames.Branches);
            builder.HasKey(branch => branch.Id);
            builder.Property(branch => branch.Name).HasMaxLength(150).IsRequired();
            builder.Property(branch => branch.IsActive).IsRequired();
            builder.HasIndex(branch => new { branch.OrganizationId, branch.Name }).IsUnique();
            builder.HasOne(branch => branch.Organization)
                .WithMany(organization => organization.Branches)
                .HasForeignKey(branch => branch.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
