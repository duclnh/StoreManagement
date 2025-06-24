using DistributedSystem.Domain.Entities.Identity;
using DistributedSystem.Persistence.Constants;
using DistributedSystem.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedSystem.Persistence.Configurations
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
