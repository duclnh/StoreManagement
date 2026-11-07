using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
    {
        public void Configure(EntityTypeBuilder<AppRole> builder)
        {
            builder.ToTable(TableNames.AppRoles);

            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired();
            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => x.TenantId);

            builder.Property(x => x.Description)
                .HasMaxLength(250).IsRequired(true);

            builder.Property(x => x.RoleCode)
                .HasMaxLength(50).IsRequired(true);


            builder.HasMany(x => x.Claims)
                .WithOne()
                .HasForeignKey(c => c.RoleId)
                .IsRequired();

            builder.HasMany(x => x.UserRoles)
               .WithOne()
               .HasForeignKey(ur => ur.RoleId)
               .IsRequired();


            builder.HasMany(x => x.Permissions)
               .WithOne()
               .HasForeignKey(p => p.RoleId)
               .IsRequired();
        }
    }
}
