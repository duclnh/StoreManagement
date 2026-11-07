using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
    {
        public void Configure(EntityTypeBuilder<LoginSession> builder)
        {
            builder.ToTable(TableNames.LoginSessions);
            builder.HasKey(session => session.Id);

            builder.Property(session => session.RefreshTokenHash)
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(session => session.UserAgent)
                .HasMaxLength(1024);
            builder.Property(session => session.IpAddress)
                .HasMaxLength(45);
            builder.Property(session => session.CreatedAtUtc).IsRequired();
            builder.Property(session => session.LastActiveAtUtc).IsRequired();
            builder.Property(session => session.ExpiresAtUtc).IsRequired();
            builder.Property(session => session.IsRevoked).IsRequired();
            builder.Property(session => session.RowVersion).IsRowVersion();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(session => new { session.UserId, session.IsRevoked, session.ExpiresAtUtc });
        }
    }
}