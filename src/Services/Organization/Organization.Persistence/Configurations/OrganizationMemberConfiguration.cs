using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organization.Domain.Entities.Members;
using BranchEntity = global::Organization.Domain.Entities.Branches.Branch;
using OrganizationEntity = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Persistence.Configurations
{
    public sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
    {
        public void Configure(EntityTypeBuilder<OrganizationMember> builder)
        {
            builder.ToTable("OrganizationMembers");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.TenantId, x.OrganizationId, x.UserId, x.BranchId }).IsUnique().HasFilter(null);
            builder.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique().HasFilter("[IsDefault] = 1");
            builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(x => new { x.TenantId, x.OrganizationId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<BranchEntity>().WithMany().HasForeignKey(x => new { x.TenantId, x.OrganizationId, x.BranchId })
                .HasPrincipalKey(x => new { x.TenantId, x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
