using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Action = Identity.Domain.Entities.Identity.Action;

namespace Identity.Persistence.Configurations
{
    public class ActionConfiguration : IEntityTypeConfiguration<Action>
    {
        public void Configure(EntityTypeBuilder<Action> builder)
        {
            builder.ToTable(TableNames.Actions);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired(true);
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.SortOrder).HasDefaultValue(null);


            //Each User can have many Permission
            builder.HasMany(a => a.Permissions)
                .WithOne()
                .HasForeignKey(p => p.ActionId).IsRequired();


            //Each user can have many ActionInFunction
            builder.HasMany(a => a.ActionInFunctions)
             .WithOne()
             .HasForeignKey(aif => aif.ActionId).IsRequired();
        }
    }
}

