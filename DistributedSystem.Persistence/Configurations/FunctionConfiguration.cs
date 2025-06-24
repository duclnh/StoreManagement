using DistributedSystem.Domain.Entities.Identity;
using DistributedSystem.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedSystem.Persistence.Configurations
{
    public class FunctionConfiguration : IEntityTypeConfiguration<Function>
    {
        public void Configure(EntityTypeBuilder<Function> builder)
        {
            builder.ToTable(TableNames.Functions);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired(true);
            builder.Property(x => x.ParrentId).HasMaxLength(50).HasDefaultValue(null);
            builder.Property(x => x.CssClasses).HasMaxLength(50).HasDefaultValue(null);
            builder.Property(x => x.Url).HasMaxLength(50).IsRequired(true);
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.SortOrder).HasDefaultValue(null);

            builder.HasMany(x => x.Permissions)
                .WithOne()
                .HasForeignKey(p => p.FunctionId)
                .IsRequired();

            builder.HasMany(x => x.ActionInFunctions)
               .WithOne()
               .HasForeignKey(aif => aif.FunctionId)
               .IsRequired();

        }
    }
}
