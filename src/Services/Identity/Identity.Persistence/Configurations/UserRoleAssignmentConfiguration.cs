using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
    {
        public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
        {
            builder.ToTable(TableNames.UserRoleAssignments);
            builder.HasKey(assignment => new
            {
                assignment.UserId,
                assignment.RoleId,
                assignment.ScopeType,
                assignment.ScopeId
            });
            builder.HasIndex(assignment => new { assignment.RoleId, assignment.ScopeType, assignment.ScopeId });
            builder.HasOne(assignment => assignment.User)
                .WithMany()
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(assignment => assignment.Role)
                .WithMany()
                .HasForeignKey(assignment => assignment.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
