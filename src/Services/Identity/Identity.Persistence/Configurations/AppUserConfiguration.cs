using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable(TableNames.AppUsers);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.IsDirector).HasDefaultValue(false);
            builder.Property(x => x.IsHeadOfDepartment).HasDefaultValue(false);
            builder.Property(x => x.ManagerId).HasDefaultValue(null);
            builder.Property(x => x.IsReceipient).HasDefaultValue(-1);

            builder.HasMany(x => x.Claims)
                .WithOne()
                .HasForeignKey(uc => uc.UserId)
                .IsRequired();

            builder.HasMany(x => x.Logins)
                .WithOne()
                .HasForeignKey(ul => ul.UserId)
                .IsRequired();

            builder.HasMany(x => x.Tokens)
                 .WithOne()
                 .HasForeignKey(ut => ut.UserId)
                 .IsRequired();

            builder.HasMany(x => x.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        }
    }
}

