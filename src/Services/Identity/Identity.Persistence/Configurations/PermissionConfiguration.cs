using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Identity.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable(TableNames.Permissions);

            builder.HasKey(x => new
            {
                x.RoleId,
                x.FunctionId,
                x.ActionId
            });

        }
    }
}

