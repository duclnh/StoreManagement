using Identity.Persistence.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public class AppUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
        {
            builder.ToTable(TableNames.AppUserRoles);
            builder.HasKey(x => new { x.RoleId, x.UserId });
        }
    }

    public class AppRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
        {
            builder.ToTable(TableNames.AppRoleClaims);
            builder.HasKey(x => x.Id);
        }
    }

    public class AppUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
        {
            builder.ToTable(TableNames.AppUserClaims);
            builder.HasKey(x => x.Id);
        }
    }

    public class AppUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
        {
            builder.ToTable(TableNames.AppUserLogins);
            builder.HasKey(x => x.UserId);
        }
    }
    public class AppUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
        {
            builder.ToTable(TableNames.AppUserTokens);
            builder.HasKey(x => x.UserId);
        }
    }
}

